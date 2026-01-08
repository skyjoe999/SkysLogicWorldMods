using LogicAPI.Data;
using LogicWorld.Server.Circuitry;
using SkysChallengeSystem.Server.Components;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysChallengeSystem.Server.LogicCode;

public class ChallengeQuestion : LogicComponent<IChallengeQuestionData>
{
    private IChallengeRoutingAccess ChallengeBoard;
    private string currentName;
    private bool initialized;

    public override void OnComponentMoved()
    {
        if (!initialized)
        {
            QueueLogicUpdate();
            return;
        }

        Disconnect();
        var board = Component.Parent;

        while (
            board != ComponentAddress.Empty &&
            board.GetLogicComponent(out var component) is not IChallengeRoutingAccess
        ) board = component.Parent;

        ChallengeBoard = board.GetLogicComponent() as IChallengeRoutingAccess;
        Connect();
    }

    protected override void DoLogicUpdate()
    {
        initialized = true;
        OnComponentMoved();
    }

    protected override void OnCustomDataUpdated()
    {
        if (currentName == Data.PegName) return;
        Disconnect();
        currentName = Data.PegName;
        Connect();
    }

    protected void Connect() => ChallengeBoard?.ConnectQuestion(currentName, Outputs[0]);
    protected void Disconnect() => ChallengeBoard?.DisconnectQuestion(currentName, Outputs[0]);

    public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex) => false;

    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues();
    public override void OnComponentDestroyed() => Disconnect();
}