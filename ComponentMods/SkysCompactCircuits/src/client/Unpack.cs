using System;
using System.Reflection;
using FancyInput;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Data.BuildingRequests;
using LogicAPI.WorldDataMutations;
using LogicUI;
using LogicWorld.Audio;
using LogicWorld.Building.Overhaul;
using LogicWorld.Building.Overhaul.Grabbing;
using LogicWorld.Building.Placement;
using LogicWorld.ClientCode;
using LogicWorld.GameStates;
using LogicWorld.Input;
using LogicWorld.Interfaces;
using LogicWorld.Modding;
using LogicWorld.Physics;
using LogicWorld.Players;
using LogicWorld.SharedCode.Components;
using SkysCompactCircuits.Client.ClientCode;
using SkysGeneralLib.Client.BuildRequests;
using UnityEngine;

namespace SkysCompactCircuits.Client;

// This code is awful and adapted directly from the de-comp of the hotbar and subassembly code
// (Which I will note, still does not use my major rewrites so it is still even more absolutely awful '^')
public static class Unpacking
{
    private class UnpackingGameState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => true;
        public override string TextID => GameStateTextID;

        // public override IEnumerable<InputTrigger> HelpScreenTriggers => HelpScreenTriggers();

        public override void OnRun() => OnStateRun();
        public override void OnExit() => OnStateExit();
        public override void OnEnter() => Start();
    }

    private static GrabbingManager ActiveGrabbingManager;
    private static bool MostRecentCanBeFlipped = false;


    public static float PersistentRotation = 0f;

    public static bool PersistentBoardsAreFlat;

    public static string GameStateTextID => "SkysCompactCircuits.Unpack";
    private static void OnStateRun()
    {
        ActiveGrabbingManager.OnRun();
        bool valueOrDefault = ActiveGrabbingManager?.Item.Placer.CurrentPlacingRules.CanBeFlipped == true;
        if (MostRecentCanBeFlipped != valueOrDefault)
        {
            MostRecentCanBeFlipped = valueOrDefault;
            GameStateManager.TriggerHelpUpdated();
        }
    }

    private static void OnStateExit()
    {
        ActiveGrabbingManager?.Dispose();
        ActiveGrabbingManager = null;
        WorldData = null;
    }
    private static void Start()
    {
        ActiveGrabbingManager?.Dispose();
        (ActiveGrabbingManager = new GrabbingManager()).StartPlacing(WorldData);
        GameStateManager.TriggerHelpUpdated();
    }
    private static PartialWorldData WorldData;

    public static void Unpack(PackedCircuit circuit)
    {
        if (circuit?.Data is null)
        {
            SoundPlayer.PlayFail();
            GameStateManager.TransitionBackToBuildingState();
        }
        WorldData = circuit.Data.PartialWorld;

        // Maybe we could account for current states (maybe... that sounds hard...)
        // (especially since most of the objects dont actually exist...)
        
        GameStateManager.TransitionTo(GameStateTextID);
    }
    public class GrabbingManager : IDisposable
    {
        private struct OutlineSkipConfig
        {
            public int OutlineSkipColliderCount;

            public float OutlineSkipTimeSeconds;
        }
        public MovingItem<int> Item;
        private StuffPlacer.MoveType MostRecentMoveType;

        private bool MostRecentCanBeFlipped;

        private float OutlineUpdateSecondsCounter;

        private int TotalMovingColliderCount;

        private bool ItemsHaveBeenInitialized;

        private OutlineSkipConfig SkipConfig = new()
        {
            OutlineSkipColliderCount = 400,
            OutlineSkipTimeSeconds = 0.1f
        };

        public StuffPlacer ActivePlacer => Item?.Placer;

        protected void FinishSetup()
        {
            Physics.SyncTransforms();
            MoveGrabbedItemsAndUpdateMovingWires(updateOutlinesAndValidity: true);
            SetupOutlineSkipValues();
            GameStateManager.TriggerHelpUpdated();
        }

        public void OnRun()
        {
            MoveGrabbedItemsAndUpdateMovingWires(ShouldUpdateOutlinesThisFrame());
            StuffPlacer.MoveType previousMoveType = Item.Placer.Ghost.PreviousMoveType;
            if (MostRecentMoveType != previousMoveType)
            {
                MostRecentMoveType = previousMoveType;
                GameStateManager.TriggerHelpUpdated();
            }

            bool canBeFlipped = Item.Placer.CurrentPlacingRules.CanBeFlipped;
            if (MostRecentCanBeFlipped != canBeFlipped)
            {
                MostRecentCanBeFlipped = canBeFlipped;
                GameStateManager.TriggerHelpUpdated();
            }

            if (Trigger.Mod.DownThisFrame())
            {
                StuffPlacer placer = Item.Placer;
                if (placer.Ghost.PreviousMoveType == StuffPlacer.MoveType.EnvironmentSmooth)
                {
                    Quaternion rotationBeforeFlipping = Item.Placer.RotationBeforeFlipping;
                    Quaternion horizontalPivotWorldspaceRotation = PlayerControllerManager.PlayerCamera.HorizontalPivotWorldspaceRotation;
                    placer.UnlockRotation();
                    placer.Info.RotationAboutUpVector = 0f;
                    placer.ExtraSystemRotationWhileSmoothPlacing = (horizontalPivotWorldspaceRotation.Inverse() * rotationBeforeFlipping).eulerAngles.y;
                }
            }
            else if (Trigger.Mod.UpThisFrame())
            {
                Item.Placer.LockRotationTo(Item.Placer.RotationBeforeFlipping);
                Item.Placer.ExtraSystemRotationWhileSmoothPlacing = 0f;
            }

            if (Trigger.Place.UpThisFrame() || Trigger.Grab.DownThisFrame() || Trigger.Clone.DownThisFrame() || RawInput.Enter.DownThisFrame())
            {
                if (!AttemptToPlaceAllGrabbedItems())
                    GameStateManager.TransitionBackToBuildingState();
            }
            else if (Trigger.Delete.DownThisFrame())
                DeleteAllGrabbedItems();
            else if (UITrigger.Back.DownThisFrame() || Trigger.CancelPlacing.DownThisFrame())
                GameStateManager.TransitionBackToBuildingState();

            bool ShouldUpdateOutlinesThisFrame()
            {
                OutlineUpdateSecondsCounter += Time.deltaTime;
                float num = (float)(TotalMovingColliderCount / SkipConfig.OutlineSkipColliderCount) * SkipConfig.OutlineSkipTimeSeconds;
                if (OutlineUpdateSecondsCounter > num)
                {
                    OutlineUpdateSecondsCounter = 0f;
                    return true;
                }

                return false;
            }
        }

        private void SetupOutlineSkipValues()
        {
            OutlineUpdateSecondsCounter = 0f;
            TotalMovingColliderCount = Item.Placer.Ghost.Colliders.Count;
        }

        private void MoveGrabbedItemsAndUpdateMovingWires(bool updateOutlinesAndValidity)
        {
            if (!ItemsHaveBeenInitialized)
            {
                if (!PlayerCaster.CameraCast((int)Masks.Environment | (int)Masks.Structure).HitSomething)
                    return;

                ItemsHaveBeenInitialized = true;
            }

            Item.Placer.PollModifierInput();
            Item.Placer.SyncCastingDataToPlayerCamera();
            Item.Placer.RunStuffPlacing(autoUpdateOutline: false);
            if (Item.Placer.Ghost.IsHidden)
                return;

            if (!updateOutlinesAndValidity)
                return;

            Item.Placer.Ghost.CollidersEnabled(value: true);
            Item.Placer.Ghost.UpdateOutline();
            Item.Placer.Ghost.CollidersEnabled(value: false);
        }

        private bool AttemptToPlaceAllGrabbedItems()
        {
            if (Item.Placer.Ghost.IsHidden)
            {
                SoundPlayer.PlayFail();
                return false;
            }

            MoveGrabbedItemsAndUpdateMovingWires(updateOutlinesAndValidity: true);

            if (Item.Placer.Ghost.WasIntersectingOnLastCheck ?? true)
            {
                SoundPlayer.PlayFail();
                return false;
            }

            PlaceAllItems();
            return true;
        }

        protected void DeleteAllGrabbedItems()
        {
            SoundPlayer.PlaySoundAt(Sounds.DeleteSomething, Item.Placer.Ghost.Transform.position);
            GameStateManager.TransitionBackToBuildingState();
        }

        public void Dispose() => Item?.Dispose();
        public void StartPlacing(PartialWorldData world)
        {

            guid = Guid.NewGuid();
            Instances.PartialWorldsManager.AddNewPartialWorldToDatabase(guid, world);

            PlacingGhost placingGhost = PlacingGhost.CreateForPartialWorldRoot(world, 0);
            StuffPlacer placer;
            if (CircuitBoardLikeTypes.IsCircuitBoardLikeInGhostWorld(placingGhost.GhostWorld, placingGhost.RootComponent))
            {
                CircuitBoard circuitBoard = (CircuitBoard)placingGhost.GhostWorld.Renderer.Entities.GetClientCode(placingGhost.RootComponent);
                Vector2Int boardSize = new Vector2Int(circuitBoard.SizeX, circuitBoard.SizeZ);
                placer = new BoardPlacer(placingGhost, boardSize);
            }
            else
            {
                PlacingRules placingRulesAt = placingGhost.GhostWorld.Dynamics.GetPlacingRulesAt(placingGhost.RootComponent);
                placer = new StuffPlacer(placingGhost, placingRulesAt);
            }

            (Item = new()).Initialize(0, PlacementData, placer);

            Item.Placer.UnlockRotation();
            FinishSetup();
        }
        Guid guid;
        private static readonly PlacementData_Standard PlacementData = (PlacementData_Standard)
            typeof(PlacementData_Standard).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0]//, [typeof(Vector3), typeof(Quaternion), typeof(PlacementType), typeof(bool)])
            .Invoke([Vector3.zero, Quaternion.identity, PlacementType.Unknown, false]);

        protected void PlaceAllItems()
        {
            PlacingGhost ghost = Item.Placer.Ghost;
            var array = new PartialWorldRootAdditionInfo
            {
                AdditionParent = ghost.PreviousMoveAddress,
                AdditionLocalPosition = ghost.GetLocalPosition(),
                AdditionLocalRotation = ghost.GetLocalRotation()
            };
            ghost.DeleteOnWorldUpdate();

            new BuildRequest_AddPartialWorld(guid, [array], []).Send();
            GameStateManager.TransitionBackToBuildingState();
        }
    }
}
