using System;
using System.Linq;
using System.Reflection;
using FancyInput;
using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicUI;
using LogicWorld;
using LogicWorld.Building.Overhaul;
using LogicWorld.GameStates;
using LogicWorld.Input;
using LogicWorld.Interfaces;
using LogicWorld.Outlines;
using LogicWorld.Physics;
using LogicWorld.Players;
using LogicWorld.References;
using LogicWorld.UI;
using SkysBoxSelect.Client.Keybindings;
using UnityEngine;

namespace SkysBoxSelect.Client;

public class SkysBoxSelect_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        CustomInput.Register<SkysBoxSelectContext, SkysBoxSelectTrigger>(Manifest.ID);
        FirstPersonInteraction.RegisterBuildingKeybinding(
            SkysBoxSelectTrigger.BoxSelect,
            () => GameStateManager.TransitionTo(BoxSelectState.SelectTextID)
        );
    }
}

public class BoxSelectState : GameState
{

    public override bool PlayerCanMoveAndLookAround => true;
    public const string SelectTextID = $"{nameof(SkysBoxSelect)}.{nameof(BoxSelectState)}";
    public override string TextID => SelectTextID;
    public static GameObject Highlighter;
    public static ComponentSelection CurrentSelection;
    public static Vector3 InitialPosition;
    public static Quaternion InitialRotation;
    public const float NormalOffset = 0.001f;
    public static float Height;
    public static Vector3 WorldDelta;

    public override void OnEnter()
    {
        var hit = PlayerCaster.CameraCast(Masks.Default | Masks.Environment | Masks.Structure | Masks.Peg);

        if (Highlighter == null)
            GenerateHighlighter();

        InitialRotation = hit.HitComponent && Instances.MainWorld.Data.Lookup(hit.cAddress) is { } component
            // "why not just use hit.RelativeNormal?" because at time of writing, the equation for it is wrong!!! 
            ? component.WorldRotation * Quaternion.FromToRotation(Vector3.up, component.WorldRotation.Inverse() * hit.WorldNormal)
            : hit.WorldNormal.y is > -0.99f and < 0.99f
                ? Quaternion.FromToRotation(Vector3.up, hit.WorldNormal)
                : hit.WorldNormal.y > 0f ? default : Quaternion.Euler(0, 0, 180);
        Highlighter.SetActive(true);
        Highlighter.transform.rotation = InitialRotation;

        InitialPosition = hit.WorldPoint + hit.WorldNormal * NormalOffset;
        CurrentSelection = [];
        Height = 0.1f;

        UpdateSelection();
        AddOutline();
    }

    public override void OnRun()
    {
        RemoveOutline();
        UpdateSelection();

        // These are just the keys I feel make the most sense to cancel with
        if (CustomInput.AnyDown(UITrigger.Back, Trigger.Undo, Trigger.Redo))
            GameStateManager.TransitionBackToBuildingState();
        // This line handles both the hold-and-drag and press-release-press modes
        else if (CustomInput.AnyDown(SkysBoxSelectTrigger.BoxSelect, Trigger.Place) || (CustomInput.UpThisFrame(SkysBoxSelectTrigger.BoxSelect) && WorldDelta.magnitude > 0.1f))
        {
            if (CurrentSelection.Count != 0)
                MultiSelector.StartWithSelection(CurrentSelection);
            else
                GameStateManager.TransitionBackToBuildingState();
        }
        else if (BuildingOperationsManager.BuildingOperationInputTriggered())
        {
            SetFirstComponentBasedOnLookingDirection();
            BuildingOperationsManager.TryDoBuildingOperation(CurrentSelection);
        }
        else if (Trigger.OpenActionWheel.DownThisFrame())
        {
            SetFirstComponentBasedOnLookingDirection();
            if (CurrentSelection.Count != 0)
                ActionWheelMenu.Instance.OpenMenuFor(CurrentSelection);
            else
                GameStateManager.TransitionBackToBuildingState();
        }
        else if (Trigger.MultiSelect.DownThisFrame())
            // We need to do some hackery to act as though we are transitioning from the normal state
            StartMultiSelectorWithSelectionAndTarget(PlayerCaster.CameraCast(Masks.Default | Masks.Environment | Masks.Structure | Masks.Peg | Masks.PlayerModel).cAddress);
        else
            AddOutline();
    }

    public static void UpdateSelection()
    {
        if (CustomInput.DownThisFrame(Trigger.NextHotbarItem))
            Height -= 0.1f;
        if (CustomInput.DownThisFrame(Trigger.PreviousHotbarItem))
            Height += 0.1f;

        var hit = PlayerCaster.CameraCast(Masks.Default | Masks.Environment | Masks.Structure | Masks.Peg);

        var box = new Bounds(new(0, Height / 2f, 0), new(0.1f, Math.Abs(Height), 0.1f));
        WorldDelta = hit.WorldPoint - InitialPosition + hit.WorldNormal * NormalOffset;
        box.Encapsulate(InitialRotation.Inverse() * WorldDelta);

        Highlighter.transform.position = InitialRotation * box.min + InitialPosition;
        Highlighter.transform.localScale = box.size;

        CurrentSelection = [..ChunkOverlaps.OverlapBox(InitialRotation * box.center + InitialPosition, box.extents, InitialRotation, Masks.Structure | Masks.Peg).Select(collider => collider.cAddress)];
        CurrentSelection.Remove(default); // no component C-0
    }


    public override void OnExit() => Highlighter.SetActive(false);
    private static void GenerateHighlighter()
    {
        Highlighter = new GameObject("Box Select Highlighter");
        Highlighter.AddComponent<MeshFilter>().mesh = Meshes.OriginCube;
        Highlighter.AddComponent<MeshRenderer>().material = MaterialsCache.StandardUnlitColorTransparent(new Color24(0x00f7ff).WithAlphaChannel(0.5f));
    }

    private static void AddOutline()
    {
        // Outliner.Outline(Highlighter, OutlineData.Valid);
        Outliner.HardOutline(CurrentSelection, OutlineData.Select);
    }

    private static void RemoveOutline()
    {
        // Outliner.RemoveOutline(Highlighter);
        Outliner.RemoveHardOutline(CurrentSelection);
    }

    private static void SetFirstComponentBasedOnLookingDirection()
    {
        HitInfo info = PlayerCaster.CameraCast((int)Masks.Environment | (int)Masks.Structure | (int)Masks.Peg | (int)Masks.PlayerModel);
        if (info.HitComponent && CurrentSelection.Contains(info.cAddress))
            CurrentSelection.FirstComponentInSelection = info.cAddress;
        else if (info.HitSomething && CurrentSelection.Count != 0)
            CurrentSelection.FirstComponentInSelection = CurrentSelection
                // compute the distance
                .Select(addr => (addr, distance: Vector3.Distance(info.WorldPoint, Instances.MainWorld.Data.Lookup(addr).WorldPosition)))
                // find the minimum
                .Aggregate((addr: ComponentAddress.Empty, distance: float.PositiveInfinity), (best, next) => best.distance > next.distance ? next : best)
                .addr is { ID: > 0 } closest ? closest : CurrentSelection.FirstComponentInSelection;
    }

    private static void StartMultiSelectorWithSelectionAndTarget(ComponentAddress target)
    {
        if (target.IsEmpty())
        {
            if (CurrentSelection.Count != 0)
                MultiSelector.StartWithSelection(CurrentSelection);
            else
                GameStateManager.TransitionBackToBuildingState();
        }
        else if (CurrentSelection.Count == 0)
            MultiSelector.StartSelectingWith(target);
        else
        {
            CurrentSelection.Add(target);
            CurrentSelection.FirstComponentInSelection = target;
            MultiSelector.StartWithSelection(CurrentSelection);
            CurrentActionTypeField.SetValue(null, 1);
            FirstComponentAddedDuringThisPassField.SetValue(null, target);
        }
    }

    private static readonly FieldInfo CurrentActionTypeField = typeof(MultiSelector).GetField("CurrentActionType", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo FirstComponentAddedDuringThisPassField = typeof(MultiSelector).GetField("FirstComponentAddedDuringThisPass", BindingFlags.Static | BindingFlags.NonPublic);
}
