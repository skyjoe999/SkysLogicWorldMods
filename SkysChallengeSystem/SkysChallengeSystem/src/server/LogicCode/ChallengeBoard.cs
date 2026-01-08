using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using SkysChallengeSystem.Server.Challenges;
using SkysChallengeSystem.Server.Components;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysChallengeSystem.Server.LogicCode;

public class ChallengeBoard :
    LogicComponent<IChallengeBoardData>,
    IChallengeFlowAccess,
    IChallengeDataAccess,
    IChallengeRoutingAccess
{
    public Challenge ActiveChallenge { get; protected set; }
    private readonly Dictionary<int, HashSet<IOutputPeg>> OutputPegs = [];

    private string previousPath;
    protected override void Initialize()
    {
        ((IChallengeFlowAccess)this).SetChallenge(Data.ChallengeFullPath);
        previousPath = Data.ChallengeFullPath;
        ActiveChallenge?.Resume(Data.RunningData);
    }

    protected override void OnCustomDataUpdated()
    {
        if (previousPath == Data.ChallengeFullPath) return;
        previousPath = Data.ChallengeFullPath;
        ((IChallengeFlowAccess)this).SetChallenge(Data.ChallengeFullPath);
        // This does not work
        // I do not know why
        // Just delete the board and then undo I guess or reload your game
        ClearConnections();
        foreach (var child in Component.EnumerateChildren())
            child.GetLogicComponent()?.OnComponentMoved();
    }

    protected override void DeserializeData(byte[] data)
    {
        base.DeserializeData(data);
        ActiveChallenge?.RunningData?.DeserializeData(Data.RunningData);
    }

    protected override void DoLogicUpdate() => Step();

    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues();

    // ----- Flow Access functions -----
    public void SetChallenge(Challenge challenge)
    {
        if (ActiveChallenge is not null) ActiveChallenge.OnPropertySet -= OnRunningDataSet;
        ActiveChallenge?.Dispose();
        ActiveChallenge = challenge;
        ActiveChallenge?.SetChallengeDataAccess(new ChallengeDataReference(Address));
        if (ActiveChallenge is not null) ActiveChallenge.OnPropertySet += OnRunningDataSet;
    }

    protected virtual void OnRunningDataSet() => Data.RunningData = ActiveChallenge.RunningData.SerializeData();

    public void Begin() => ActiveChallenge?.Begin();
    public void Cancel() => ActiveChallenge?.Cancel();
    public void Step() => ActiveChallenge?.Step();

    // ----- Data Access functions -----
    protected List<IChallengeDisplay> _ChallengeDisplays { get; } = [];
    protected List<IChallengeErrorDisplay> _ChallengeErrorDisplays { get; } = [];
    public IReadOnlyList<IChallengeDisplay> ChallengeDisplays => _ChallengeDisplays;

    public bool GetAnswer(int index) => Inputs[index].On;

    public void SetQuestion(int index, bool value)
    {
        if (!OutputPegs.TryGetValue(index, out var set)) return;
        foreach (var peg in set) peg.On = value;
    }

    public void RegisterDisplay(IChallengeDisplay display) => _ChallengeDisplays.Add(display);

    public void RegisterErrorDisplay(IChallengeErrorDisplay display) => _ChallengeErrorDisplays.Add(display);

    public void RemoveDisplay(IChallengeDisplay display) => _ChallengeDisplays.Remove(display);

    public void RemoveErrorDisplay(IChallengeErrorDisplay display) => _ChallengeErrorDisplays.Remove(display);

    public void SetErrorMessage(string errorMessage)
    {
        if (errorMessage.IsNullOrEmpty())
            foreach (var display in _ChallengeErrorDisplays)
                display.ClearError();
        else
            foreach (var display in _ChallengeErrorDisplays)
                display.SetError(errorMessage);
    }

    public void ClearDisplays()
    {
        foreach (var display in _ChallengeDisplays)
            display.Clear();
    }

    // ----- Routing Access functions -----
    public void ConnectAnswer(string name, IInputPeg peg)
    {
        var index = ActiveChallenge?.Record.AnswerNames?.ToList().FindIndex(s => s == name) ?? -1;
        if (index == -1) return;
        Inputs[index].AddSecretLinkWith(peg);
    }

    public void ConnectQuestion(string name, IOutputPeg peg)
    {
        var index = ActiveChallenge?.Record.QuestionNames?.ToList().FindIndex(s => s == name) ?? -1;
        if (index == -1) return;
        if (!OutputPegs.ContainsKey(index)) OutputPegs[index] = [];
        OutputPegs[index].Add(peg);
    }

    public void DisconnectAnswer(string name, IInputPeg peg)
    {
        var index = ActiveChallenge?.Record.AnswerNames?.ToList().FindIndex(s => s == name) ?? -1;
        if (index == -1) return;
        Inputs[index].RemoveSecretLinkWith(peg);
    }

    public void DisconnectQuestion(string name, IOutputPeg peg)
    {
        var index = ActiveChallenge?.Record.QuestionNames?.ToList().FindIndex(s => s == name) ?? -1;
        if (index == -1) return;
        if (OutputPegs.TryGetValue(index, out var outputPeg)) outputPeg.Remove(peg);
    }

    public void ClearConnections()
    {
        foreach (var peg in Inputs) peg.RemoveAllSecretLinks();
        OutputPegs.Clear();
    }
}