using System.Collections.Generic;
using LogicAPI.Data;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysChallengeSystem.Server.Components;

public class ChallengeDataReference(ComponentAddress Address) : IChallengeDataAccess
{
    public void LogicUpdate() => deref?.LogicUpdate();

    public void QueueLogicUpdate() => deref?.QueueLogicUpdate();

    public bool GetAnswer(int index) => deref?.GetAnswer(index) ?? false;

    public void SetQuestion(int index, bool value) => deref?.SetQuestion(index, value);

    public IReadOnlyList<IChallengeDisplay> ChallengeDisplays => deref?.ChallengeDisplays;

    public void RegisterDisplay(IChallengeDisplay display) => deref?.RegisterDisplay(display);

    public void RemoveDisplay(IChallengeDisplay display) => deref?.RemoveDisplay(display);

    public void RegisterErrorDisplay(IChallengeErrorDisplay display) => deref?.RegisterErrorDisplay(display);

    public void RemoveErrorDisplay(IChallengeErrorDisplay display) => deref?.RemoveErrorDisplay(display);

    public void SetErrorMessage(string errorMessage) => deref?.SetErrorMessage(errorMessage);

    public void ClearDisplays() => deref?.ClearDisplays();

    private IChallengeDataAccess deref => Address.GetLogicComponent() as IChallengeDataAccess;
}
