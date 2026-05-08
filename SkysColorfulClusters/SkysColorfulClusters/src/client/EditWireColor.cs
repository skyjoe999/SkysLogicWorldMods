using System.Collections.Generic;
using System.Linq;
using EccsGuiBuilder.Client.Layouts.Elements;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using FancyInput;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicUI;
using LogicUI.ColorChoosing;
using LogicUI.MenuParts;
using LogicUI.MenuTypes;
using LogicWorld.Audio;
using LogicWorld.Building.Overhaul;
using LogicWorld.GameStates;
using LogicWorld.Input;
using LogicWorld.Interfaces;
using LogicWorld.Physics;
using LogicWorld.Players;
using SkysColorfulClusters.Client.Keybindings;
using SkysColorfulClusters.Shared;
using SkysColorfulClusters.Shared.Packets;
using SkysGeneralLib.Client.TypeExtensions;

namespace SkysColorfulClusters.Client;

public class EditWireColor : ToggleableSingletonMenu<EditWireColor>, IAssignMyFields
{
    public static void Build()
    {
        WS.window("SkysColorfulClusters.EditWireColorWindow")
            .setYPosition(870)
            .setLocalizedTitle("SkysColorfulClusters.Gui.EditWireColor.Title")
            .configureContent(content => content
                .layoutVertical()
                .addContainer("Middle", container => container
                    .layoutHorizontal()
                    .add(WS.textLine.setLocalizationKey("SkysColorfulClusters.Gui.EditWireColor.Off"))
                    .add(WS.colorPicker
                        .injectionKey(nameof(OffColorPicker))
                        .fixedSize(210, 70)
                    )
                    .add(WS.textLine.setLocalizationKey("SkysColorfulClusters.Gui.EditWireColor.On"))
                    .add(WS.colorPicker
                        .injectionKey(nameof(OnColorPicker))
                        .fixedSize(210, 70)
                    )
                )
                .addContainer("Bottom", container => container
                    .layoutHorizontal()
                    .add(WS.button
                        .add<ButtonLayout>()
                        .setLocalizationKey("SkysColorfulClusters.Gui.EditWireColor.Cancel")
                        .injectionKey(nameof(CancelButton))
                    )
                    .add(WS.button
                        .add<ButtonLayout>()
                        .setLocalizationKey("SkysColorfulClusters.Gui.EditWireColor.Reset")
                        .injectionKey(nameof(ResetButton))
                    )
                )
            )
            .add<EditWireColor>()
            .build();
    }

    //Instance part:
    [AssignMe]
    public ColorChooser OffColorPicker;
    [AssignMe]
    public ColorChooser OnColorPicker;
    [AssignMe]
    public HoverButton ResetButton;
    [AssignMe]
    public HoverButton CancelButton;

    public ClusterColor PickedColor
    {
        get => new(OffColorPicker.Color24, OnColorPicker.Color24);
        set => (OffColorPicker.Color24, OnColorPicker.Color24) = value;
    }

    public bool IsDefaultColors => PickedColor == ClusterColor.Default;
    public bool HasReset;
    // This would be where I'd keep my undo functionality... IF I HAD ANY!
    public void SetTempColor() => SendColor();
    public void SendColor()
    {
        if (EditingPegs.Count == 1)
        {
            ClusterColor? color = IsDefaultColors ? null : PickedColor;
            Instances.SendData.Send(new ChangeClusterColorRequest()
            {
                clusterColors = [(EditingPegs[0], color)],
            });
            return;
        }

        Instances.SendData.Send(new ChangeClusterColorRequest()
        {
            clusterColors = [.. EditingPegs.Select((a, i) => (a, GetColor(i)))],
        });
        ChangeClusterColorRequest._ClusterColor? GetColor(int index) => !IsDefaultColors ? PickedColor : HasReset ? null : InitialColors[index];
    }

    public override void Initialize()
    {
        base.Initialize();

        //Setup events and handlers:
        OffColorPicker.OnColorChange24 += _ =>
        {
            HasReset = false;
            SetTempColor();
        };
        OnColorPicker.OnColorChange24 += _ =>
        {
            HasReset = false;
            SetTempColor();
        };
        ResetButton.OnClickBegin += () =>
        {
            HasReset = true;
            PickedColor = ClusterColor.Default;
            SetTempColor();
        };
        CancelButton.OnClickBegin += () =>
        {
            HasReset = false;
            PickedColor = EditingPegs.Count == 1 ? InitialColors[0] ?? ClusterColor.Default : ClusterColor.Default;
            SetTempColor();
            GameStateManager.TransitionBackToBuildingState();
        };
    }
    private static List<PegAddress> EditingPegs = [];
    private static ClusterColor?[] InitialColors;

    public static string GameStateTextID => "SkysColorfulClusters.EditWireColor";
    public class EditWireColorState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => false;
        public override string TextID => GameStateTextID;
        public override void OnEnter()
        {
            if (EditingPegs.Count == 0)
            {
                SoundPlayer.PlayFail();
                GameStateManager.TransitionBackToBuildingState();
                return;
            }
            Instance.HasReset = false;
            InitialColors = EditingPegs.Select(ClusterColorManager.GetClusterColor).ToArray();
            if (InitialColors.AllElementsAreTheSame())
            {
                Instance.PickedColor = InitialColors[0] ?? ClusterColor.Default;
                if (Instance.IsDefaultColors)
                    Instance.HasReset = true;
            }
            else
                Instance.PickedColor = ClusterColor.Default;

            // SavedColorManagerInstance.ColorsFileKey = "Wires";
            // SavedColorManagerInstance.ReloadMenuFromFile();
            ShowMenu();
        }
        public override void OnRun()
        {
            if (CustomInput.AnyDown(
                Trigger.Delete,
                UITrigger.Back,
                Trigger.CancelPlacing,
                Trigger.EditComponent,
                Trigger.Undo,
                Trigger.Redo
            ))
                GameStateManager.TransitionBackToBuildingState();
        }
        public override void OnExit()
        {
            EditingPegs.Clear();
            HideMenu();
        }
    }

    public static bool TrySetEditingPeg()
    {
        EditingPegs.Clear();

        var hitInfo = PlayerCaster.CameraCast(Masks.Environment | Masks.Structure | Masks.Peg | Masks.Wire);
        var targetPeg = hitInfo.pAddress;
        if (targetPeg.IsEmpty() && hitInfo.wAddress.IsNotEmpty())
        {
            var wAddress = Instances.MainWorld.Data.AllWires.GetValueOrDefault(hitInfo.wAddress)?.Point1;
            if (wAddress?.IsInputAddress() ?? false) // ensure we always get the output peg
                wAddress = Instances.MainWorld.Data.AllWires.GetValueOrDefault(hitInfo.wAddress)?.Point2;
            targetPeg = wAddress ?? default;
        }
        if (targetPeg.IsEmpty())
            return false;
        EditingPegs = [targetPeg];
        return true;
    }
    private class EditWireColorOperation : BuildingOperation
    {
        public override InputTrigger OperationStarter => SkysColorfulClustersTrigger.EditWireColor;
        public override string IconHexCode => "f044";
        public override void BeginOperationOn(ComponentSelection selection)
        {
            if (!TrySetEditingPeg() || !selection.Contains(EditingPegs[0].ComponentAddress) || selection.Count != 1)
            {
                var inputs = selection.SelectMany(a => a.GetComponent().Data.InputInfos.Select((_, i) => new PegAddress(a, i, PegType.Input)));
                var outputs = selection.SelectMany(a => a.GetComponent().Data.OutputInfos.Select((_, i) => new PegAddress(a, i, PegType.Output)));
                EditingPegs = [.. inputs, .. outputs];
            }
            if (GameStateManager.CurrentStateID != GameStateTextID)
                GameStateManager.TransitionTo(GameStateTextID);
        }

        public override bool CanOperateOn(ComponentSelection selection) =>
            selection.Any(a => a.GetComponent() is IComponentInWorld comp &&
            (comp.Data.InputCount != 0 || comp.Data.OutputCount != 0));
    }

    // private static SavedColorManager SavedColorManagerInstance;
    // [HarmonyPatch(typeof(SavedColorManager), MethodType.Constructor)]
    // static class SavedColorManagerGetter { static void Prefix(SavedColorManager __instance) => SavedColorManagerInstance = __instance; }
}

