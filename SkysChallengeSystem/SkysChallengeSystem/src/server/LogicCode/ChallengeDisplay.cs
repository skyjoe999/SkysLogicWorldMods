using LogicAPI.Data;
using LogicWorld.Server.Circuitry;
using SkysChallengeSystem.Server.Components;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysChallengeSystem.Server.LogicCode;

public class ChallengeDisplay : LogicComponent<IChallengeDisplayData>, IChallengeDisplay<string>, IChallengeErrorDisplay
{
    private ComponentAddress previousBoard;

    public override void OnComponentMoved()
    {
        // BUG: Display gets cleared on world load and when undoing a delete
        var board = Component.Parent;

        while (
            board != ComponentAddress.Empty &&
            board.GetLogicComponent(out var component) is not IChallengeDataAccess
        ) board = component.Parent;

        if (previousBoard == board) return;
        (previousBoard.GetLogicComponent() as IChallengeDataAccess)?.RemoveDisplay(this);
        (previousBoard.GetLogicComponent() as IChallengeDataAccess)?.RemoveErrorDisplay(this);

        previousBoard = board;
        (board.GetLogicComponent() as IChallengeDataAccess)?.RegisterDisplay(this);
        (board.GetLogicComponent() as IChallengeDataAccess)?.RegisterErrorDisplay(this);
        Clear();
    }

    public override void OnComponentDestroyed()
    {
        (previousBoard.GetLogicComponent() as IChallengeDataAccess)?.RemoveDisplay(this);
        (previousBoard.GetLogicComponent() as IChallengeDataAccess)?.RemoveErrorDisplay(this);
    }

    public void SetError(string errorMessage)
    {
        if (Data.DisplayText == IChallengeDisplayData.SuperSecretTemporaryString) return;
        Data.DisplayText = errorMessage;
        Data.IsError = true;
    }

    public void SetValue(string value)
    {
        if (Data.DisplayText == IChallengeDisplayData.SuperSecretTemporaryString) return;
        Data.DisplayText = value;
        Data.IsError = false;
    }

    public void ClearError()
    {
        if (Data.IsError) Clear();
    }

    public void Clear()
    {
        if (Data.DisplayText == IChallengeDisplayData.SuperSecretTemporaryString) return;
        Data.DisplayText = "";
        Data.IsError = false;
    }

    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues();
}
