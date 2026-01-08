using EccsGuiBuilder.Client.Layouts.Elements;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using LogicUI.MenuParts;
using LogicWorld.UI;
using SkysChallengeSystem.Shared.ComponentDataDefs;

namespace SkysChallengeSystem.Client.EditGUI;

public class EditChallengeDisplay : EditComponentMenu<IChallengeDisplayData>, IAssignMyFields
{
    public static void initialize()
    {
        WS.window("SkysChallengeSystemEditChallengeDisplayWindow")
            .setYPosition(870)
            .configureContent(wrapper => wrapper
                .layoutVertical()
                .addContainer("FontSize", content => content
                    .layoutVerticalInner(spacing: 10)
                    .add(WS.textLine
                        .setLocalizationKey("SkysChallengeSystem.Gui.ChallengeDisplay.FontSizeLabel")
                        .setFontSize(25)
                    )
                    .add(WS.slider
                        .injectionKey(nameof(valueSlider))
                        .setMin(IChallengeDisplayData.ChallengeDisplayMinFontSize)
                        .setMax(IChallengeDisplayData.ChallengeDisplayMaxFontSize)
                        .setInterval(IChallengeDisplayData.ChallengeDisplayFontSizeStep)
                        .setDecimalDigitsToDisplay(1)
                        .fixedSize(200, 38)
                    )
                )
                .addContainer("DisplayType", content => content
                    .layoutHorizontalInner()
                    .add(WS.button
                        .setLocalizationKey("SkysChallengeSystem.Gui.ChallengeDisplay.NormalButtonLabel")
                        .injectionKey(nameof(normalButton))
                        .add<ButtonLayout>()
                    )
                    .add(WS.button
                        .setLocalizationKey("SkysChallengeSystem.Gui.ChallengeDisplay.DescriptionButtonLabel")
                        .injectionKey(nameof(descriptionButton))
                        .add<ButtonLayout>()
                    )
                )
            )
            .add<EditChallengeDisplay>()
            .build();
    }

    //Instance part:
    [AssignMe] public InputSlider valueSlider;
    [AssignMe] public HoverButton normalButton;
    [AssignMe] public HoverButton descriptionButton;

    public override void Initialize()
    {
        base.Initialize();

        //Setup events and handlers:
        valueSlider.OnValueChanged += value =>
        {
            foreach (var entry in ComponentsBeingEdited) entry.Data.FontSize = value;
        };
        normalButton.OnClickBegin += () =>
        {
            foreach (var entry in ComponentsBeingEdited)
                if (entry.Data.DisplayText == IChallengeDisplayData.SuperSecretTemporaryString)
                    entry.Data.DisplayText = "";
        };
        descriptionButton.OnClickBegin += () =>
        {
            foreach (var entry in ComponentsBeingEdited)
                entry.Data.DisplayText = IChallengeDisplayData.SuperSecretTemporaryString;
        };
    }

    protected override void OnStartEditing()
    {
        var data = FirstComponentBeingEdited.Data;
        valueSlider.SetValueWithoutNotify(data.FontSize);
    }
}
