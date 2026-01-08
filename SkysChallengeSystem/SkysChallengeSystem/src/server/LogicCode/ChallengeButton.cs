using System;
using LogicAPI.Data;
using LogicWorld.Server.Circuitry;
using SkysChallengeSystem.Server.Components;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysChallengeSystem.Server.LogicCode;

public class ChallengeButton : LogicComponent<IChallengeButtonData>
{
    protected override void OnCustomDataUpdated() => QueueLogicUpdate();

    private bool previousDown;
    protected ComponentAddress ActiveBoard;

    protected override void DoLogicUpdate()
    {
        if (Data.ButtonDown == previousDown) return;
        if (!(previousDown = Data.ButtonDown)) return;
        switch (Data.ButtonType)
        {
            case ButtonTypes.Start:
                (ActiveBoard.GetLogicComponent() as IChallengeFlowAccess)?.Begin();
                break;
            case ButtonTypes.Cancel:
                (ActiveBoard.GetLogicComponent() as IChallengeFlowAccess)?.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void OnComponentMoved()
    {
        var board = Component.Parent;

        while (
            board != ComponentAddress.Empty &&
            board.GetLogicComponent(out var component) is not IChallengeFlowAccess
        ) board = component.Parent;
        ActiveBoard = board;
    }

    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues();
}