using System.Collections.Generic;
using System.Linq;
using EccsGuiBuilder.Client.Layouts.Elements;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using LogicLocalization;
using LogicUI.MenuParts;
using LogicWorld.UI;
using SkysChallengeSystem.Client.ClientCode;
using TMPro;
using JimmysUnityUtilities;
using SkysChallengeSystem.Shared.ComponentDataDefs;

namespace SkysChallengeSystem.Client.EditGUI;

public class EditChallengePeg : EditComponentMenu<IChallengePegData>, IAssignMyFields
{
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
            .add<EditChallengePeg>()
            .build();
    }

    //Instance part:
    [AssignMe] public HoverButton leftButton;
    [AssignMe] public HoverButton rightButton;
    [AssignMe] public LocalizedTextMesh text;


    // private ChallengeRecord challenge;
    private string[] pegNames;
    private int selectedIndex;
    private bool edited;
    private bool isQuestion;

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
        else if (selectedIndex <= 0 && !right) selectedIndex = pegNames.Length - 1;
        else if (selectedIndex >= pegNames.Length - 1 && right) selectedIndex = 0;
        else selectedIndex += right ? 1 : -1;
        edited = true;
        text.SetLocalizationKey(pegNames[selectedIndex], true);
    }

    protected override void OnStartEditing()
    {
        edited = false;
        isQuestion = ((ChallengePeg)FirstComponentBeingEdited.ClientCode).isQuestion;

        var data = FirstComponentBeingEdited.Data;
        var board = ((IHasChallengeBoardParent)FirstComponentBeingEdited.ClientCode).ChallengeBoard;
        pegNames = board is not null
            ? isQuestion
                ? ChallengeManager.GetRecord(board.ChallengeFullPath)?.QuestionNames ?? []
                : ChallengeManager.GetRecord(board.ChallengeFullPath)?.AnswerNames ?? []
            : [];
        selectedIndex = pegNames.ToList().FindIndex(s => s == data.PegName);
        text.SetLocalizationKey(
            data.PegName.IsNullOrWhiteSpace() ? "No Challenge Selected" : data.PegName,
            true
        );
        rightButton.enabled = leftButton.enabled = pegNames.Length != 0;
    }

    protected override void OnClose()
    {
        if (!edited) return; // useful for multi-select or invalid starting 
        foreach (var entry in ComponentsBeingEdited)
            entry.Data.PegName = pegNames[selectedIndex];
    }

    protected override bool CanEditCollection(IReadOnlyList<TypedEditingComponentInfo<IChallengePegData>> collection)
    {
        var allSame = collection.Select(info => (info.ClientCode as ChallengePeg)?.isQuestion).AllElementsAreTheSame();
        return base.CanEditCollection(collection) && allSame;
    }
}
