using System.Linq;
using EccsGuiBuilder.Client.Layouts.Elements;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using JimmysUnityUtilities;
using LogicAPI.Data.BuildingRequests;
using LogicLocalization;
using LogicUI.MenuParts;
using LogicWorld.UI;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Client.BuildRequests;
using TMPro;

namespace SkysChallengeSystem.Client.EditGUI;

public class EditChallengeBoard : EditComponentMenu<IChallengeBoardData>, IAssignMyFields
{
    // private static WindowWrapper window;

    public static void initialize()
    {
        WS.window("SkysChallengeSystemEditChallengeInputWindow")
            .setYPosition(870)
            .setResizeableHorizontal()
            .configureContent(content => content
                // .layoutHorizontal(expandChildThickness:false)
                .layoutGrowElementHorizontal(elementIndex: IndexHelper.nth(1))
                .add(WS.button
                    .add<ButtonLayout>()
                    .setLocalizationKey("SkysChallengeSystem.Gui.Left")
                    .injectionKey(nameof(leftButton))
                )
                .add(WS.textLine
                    .configureTMP(ugui =>
                    {
                        ugui.horizontalAlignment = HorizontalAlignmentOptions.Center;
                        ugui.overflowMode = TextOverflowModes.Ellipsis;
                        ugui.alignment = TextAlignmentOptions.Center;
                    })
                    .injectionKey(nameof(text))
                )
                .add(WS.button
                    .add<ButtonLayout>()
                    .setLocalizationKey("SkysChallengeSystem.Gui.Right")
                    .injectionKey(nameof(rightButton))
                )
            )
            .add<EditChallengeBoard>()
            .build();
    }

    //Instance part:
    [AssignMe] public HoverButton leftButton;
    [AssignMe] public HoverButton rightButton;
    [AssignMe] public LocalizedTextMesh text;

    private string[] challenges;
    private int selectedIndex;
    private bool edited;

    public override void Initialize()
    {
        base.Initialize();
        // //Setup events and handlers:
        leftButton.OnClickEnd += () => cycle(false);
        rightButton.OnClickEnd += () => cycle(true);
    }

    private void cycle(bool right)
    {
        if (selectedIndex == -1) selectedIndex = 0;
        else if (selectedIndex <= 0 && !right) selectedIndex = challenges.Length - 1;
        else if (selectedIndex >= challenges.Length - 1 && right) selectedIndex = 0;
        else selectedIndex += right ? 1 : -1;
        edited = true;
        text.SetLocalizationKey(challenges[selectedIndex], true);
    }

    protected override void OnStartEditing()
    {
        var data = FirstComponentBeingEdited.Data;
        edited = false;
        challenges = ChallengeManager.GetChallengePaths();
        selectedIndex = challenges.ToList().FindIndex(s => s == data.ChallengeFullPath);
        // leftButton.enabled = false;
        text.SetLocalizationKey(
            data.ChallengeFullPath.IsNullOrWhiteSpace() ? "No Challenge Selected" : data.ChallengeFullPath,
            true
        );
    }

    protected override void OnClose()
    {
        if (!edited) return; // useful for multi-select or invalid starting 
        var value = ChallengeManager.GetRecord(challenges[selectedIndex]).AnswerCount;
        foreach (var entry in ComponentsBeingEdited)
        {
            new BuildRequest_ChangeDynamicComponentPegCounts(entry.Address, value, 0).Send();
            entry.Data.ChallengeFullPath = challenges[selectedIndex];
        }
    }
}
