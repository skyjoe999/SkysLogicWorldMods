using EccsGuiBuilder.Client.Layouts.Elements;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using LogicUI.MenuParts;
using LogicWorld.UI;
using SkysChallengeSystem.Shared.ComponentDataDefs;

namespace SkysChallengeSystem.Client.EditGUI;

public class EditChallengeButton : EditComponentMenu<IChallengeButtonData>, IAssignMyFields
{
    public static void initialize()
    {
        WS.window("SkysChallengeSystemEditChallengeButtonWindow")
            .setYPosition(870)
            .configureContent(content => content
                .layoutHorizontal()
                .add(WS.button
                    .setLocalizationKey("SkysChallengeSystem.Gui.ChallengeButton.StartButtonLabel")
                    .injectionKey(nameof(startButton))
                    .add<ButtonLayout>()
                )
                .add(WS.button
                    .setLocalizationKey("SkysChallengeSystem.Gui.ChallengeButton.CancelButtonLabel")
                    .injectionKey(nameof(cancelButton))
                    .add<ButtonLayout>()
                )
            )
            .add<EditChallengeButton>()
            .build();
    }

    //Instance part:
    [AssignMe] public HoverButton startButton;
    [AssignMe] public HoverButton cancelButton;

    public override void Initialize()
    {
        base.Initialize();

        //Setup events and handlers:
        startButton.OnClickBegin += () =>
        {
            foreach (var entry in ComponentsBeingEdited) entry.Data.ButtonType = ButtonTypes.Start;
        };
        cancelButton.OnClickBegin += () =>
        {
            foreach (var entry in ComponentsBeingEdited) entry.Data.ButtonType = ButtonTypes.Cancel;
        };
    }

    protected override void OnStartEditing()
    {
    }
}
