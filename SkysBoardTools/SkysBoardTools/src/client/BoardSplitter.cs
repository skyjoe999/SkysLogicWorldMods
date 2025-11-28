using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EccsLogicWorldAPI.Shared.AccessHelper;
using FancyInput;
using JetBrains.Annotations;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Data.BuildingRequests;
using LogicAPI.WorldDataMutations;
using LogicUI;
using LogicWorld.Audio;
using LogicWorld.Building;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.GameStates;
using LogicWorld.Input;
using LogicWorld.Interfaces;
using LogicWorld.Outlines;
using LogicWorld.Physics;
using LogicWorld.Players;
using SkysGeneralLib.Client.TypeExtensions;
using SkysBoardTools.Client.Keybindings;
using SkysGeneralLib.Client.BuildRequests;
using UnityEngine;

namespace SkysBoardTools.client;

public static class BoardSplitter
{
    public static string GameStateTextID => "SkysBoardTools.BoardSplitting";

    // TODO: Help screen
    private static IEnumerable<InputTrigger> _HelpScreenTriggers() => [];
    private static readonly List<ComponentType> UnSplittableTypes = new(); // Modding considerations? In LW? Unheard-of
    public static SplitManager splitting;

    public static void AddUnSplittableType(ComponentType type)
    {
        if (!UnSplittableTypes
                .Contains(type)) // should always be true but might save someone a headache at a one-time cost
            UnSplittableTypes.Add(type);
    }

    [UsedImplicitly]
    public class JoinBoardsOperation : BuildingOperation
    {
        public override InputTrigger OperationStarter => SkysBoardToolsTrigger.SplitBoards;
        // public override string IconHexCode => "f047";

        public override bool CanOperateOn(ComponentSelection selection)
        {
            // Very might never be called with 2+ selected if i cant get multi select to work >:/
            // (except for in the radial menu... you just cant actually start it)
            if (selection.Count != 1) return false;
            foreach (var address in selection)
            {
                if (address.GetClientCode() is not CircuitBoard clientCode) continue;
                var type = clientCode.Component.Data.Type;
                if (UnSplittableTypes.Contains(type)) continue;
                return true;
            }

            return false;
        }

        public override void BeginOperationOn(ComponentSelection selection)
        {
            if (selection.Count != 1)
                return;

            foreach (var address in selection)
            {
                if (address.GetClientCode() is not CircuitBoard clientCode) continue;
                var type = clientCode.Component.Data.Type;
                if (UnSplittableTypes.Contains(type)) continue;
                GameStateManager.TransitionTo(GameStateTextID);
                splitting = new SplitManager(clientCode);
                return;
            }
            SoundPlayer.PlayFail();
        }
    }

    // public static void SplitBoardsTransition()
    // {
    //     if (GameStateManager.CurrentStateID == GameStateTextID)
    //     {
    //         GameStateManager.TransitionBackToBuildingState(); // No clue if this can be called
    //         return;
    //     }

    //     var hitInfo = PlayerCaster.CameraCast(Masks.Environment | Masks.Structure | Masks.Peg | Masks.Wire);
    //     if (
    //         !hitInfo.HitComponent ||
    //         hitInfo.cAddress.GetClientCode() is not CircuitBoard clientCode ||
    //         UnSplittableTypes.Contains(clientCode.Component.Data.Type)
    //     )
    //     {
    //         SoundPlayer.PlayFail();
    //         return;
    //     }
    //     splitting = new SplitManager(clientCode);
    //     GameStateManager.TransitionTo(GameStateTextID);
    // }

    [UsedImplicitly]
    private class SplitBoardsState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => true;

        public override bool ShowHotbarWhileStateActive => false;

        public override string TextID => GameStateTextID;

        public override void OnRun() => splitting.OnRun();

        public override void OnExit()
        {
            splitting.Dispose();
            splitting = null;
        }

        public override IEnumerable<InputTrigger> HelpScreenTriggers => _HelpScreenTriggers();
    }

    public class SplitManager : IDisposable
    {
        private readonly CircuitBoard Board;
        private readonly IRenderedEntity BoardRender;

        private Outline RenderOutline;

        // private CircuitBoard OtherBoard;
        // private bool areFlipped;
        private bool isValid;

        // private bool transposeSize; // Swap SizeX and SizeZ for the other board
        // private Vector2 correctedPosition;
        private bool splitAlongHorizontal;
        private int newSize;
        private int oldSize;

        private bool keepAsRoot;
        private ComponentAddress[] componentsOnOldBoard;

        public SplitManager(CircuitBoard mainBoard)
        {
            Board = mainBoard;
            BoardRender = Instances.MainWorld.Renderer.Entities.GetBlockEntitiesAt(Board.Address)[0];
            Outliner.Outline(mainBoard.Address, OutlineData.LookingAtComponent);
        }


        public void Dispose()
        {
            Outliner.RemoveOutline(Board.Address);
            // if (OtherBoard != null)
            //     Outliner.RemoveOutline(OtherBoard.Address);
        }

        public void OnRun()
        {
            // Cancel
            if (CustomInput.AnyDown(
                    Trigger.Delete,
                    UITrigger.Back,
                    Trigger.CancelPlacing,
                    Trigger.Undo,
                    Trigger.Redo,
                    SkysBoardToolsTrigger.JoinBoards
                ))
                Cancel();
            // Finish
            isValid = oldSize != newSize & 0 != newSize;
            if (CustomInput.AnyDown(
                    Trigger.Place,
                    SkysBoardToolsTrigger.SplitBoards
                ))
                if (isValid)
                    Finish();
                else
                    Cancel();
            // Update
            if (!TryCalculateSplit())
            {
                // Outliner.RemoveOutline(OtherBoard.Address);
                // isValid = false; // maybe that fixes the crash? idk
                // OtherBoard = null;
            }
        }

        private void Cancel()
        {
            // Let dispose handle the rest
            SoundPlayer.PlaySoundGlobal(Sounds.DeleteSomething);
            GameStateManager.TransitionBackToBuildingState();
        }

        private void Finish()
        {
            decideParent();

            var componentsOnNewBoard = Board.Component.EnumerateChildren()
                .Except(componentsOnOldBoard).ToArray();
            if (!keepAsRoot)
                (componentsOnOldBoard, componentsOnNewBoard) = (componentsOnNewBoard, componentsOnOldBoard);
            int fullSize = splitAlongHorizontal ? Board.SizeZ : Board.SizeX;
            var localOffset = splitAlongHorizontal
                ? Vector3.zero with { z = newSize }
                : Vector3.zero with { x = newSize };
            var clone = new SingleComponentCloneData()
            {
                SourceAddress = Board.Address,
                LocalPosition = (keepAsRoot ? localOffset * 0.3f : Vector3.zero),
                LocalRotation = Quaternion.identity,
                ParentAddress = Board.Address
            };

            // Did I really need to format it like this? Absolutely not!
            List<BuildRequest> requests =
            [
                // Set data for new board
                new BuildRequest_UpdateComponentCustomData(
                    Board.Address,
                    getDataAtSize(keepAsRoot ? fullSize - newSize : newSize)
                ),
                // Move Components on new board for new board
                ..keepAsRoot
                    ? componentsOnNewBoard.Convert(addr =>
                        new BuildRequest_RepositionComponent(
                            addr,
                            localOffset * -0.3f
                        ))
                    : [],
                // Move Components not on new board for new board
                ..componentsOnOldBoard.Convert(addr =>
                    new BuildRequest_UpdateComponentPositionRotationParent(
                        addr,
                        addr.GetComponent().WorldPosition,
                        addr.GetComponent().WorldRotation,
                        ComponentAddress.Empty
                    )),
                // Clone the damn board
                new BuildRequest_CloneComponents([clone], []),
                // Reset the components we just moved 
                ..componentsOnOldBoard.Convert(addr =>
                    new BuildRequest_UpdateComponentPositionRotationParent(
                        addr,
                        addr.GetComponent().LocalPosition,
                        addr.GetComponent().LocalRotation,
                        Board.Address
                    )),
                // Reset the components we just moved  and are about to delete!
                // (otherwise undoing will mess up the wire position)
                // ((and maybe sockets if I think about it))
                ..componentsOnOldBoard.Convert(addr =>
                    new BuildRequest_UpdateComponentPositionRotationParent(
                        addr,
                        addr.GetComponent().LocalPosition,
                        addr.GetComponent().LocalRotation,
                        Board.Address
                    )),
                // Set the size of the original board
                new BuildRequest_UpdateComponentCustomData(
                    Board.Address,
                    getDataAtSize(keepAsRoot ? newSize : fullSize - newSize)
                )
            ];
            if (componentsOnNewBoard.Length != 0)
                // Delete the components we only want on the new board
                requests.Add(new BuildRequest_RemoveComponentsAndChildrenAndAttachedWires(componentsOnNewBoard));
            if (!keepAsRoot)
                // Move the old board into place
                requests.Add(new BuildRequest_RepositionComponent(
                    Board.Address,
                    Board.Component.LocalRotation * localOffset * 0.3f
                ));
            requests.SendAllRequests();
            GameStateManager.TransitionBackToBuildingState();
        }


        private void SetSize(int size)
        {
            if (splitAlongHorizontal) Board.SizeZ = size;
            else Board.SizeX = size;
        }

        private void decideParent()
        {
            SetSize(newSize);
            DataUpdateInfo.Invoke(Board, []);
            var colliders =
                ColliderCollection.Create(
                    Instances.MainWorld.Renderer.EntityColliders.GetCollidersOfComponent(Board.Address)
                );
            keepAsRoot = Board.Component.Parent == ComponentAddress.Empty
                ? Intersections.CollidersTouchingEnvironment(colliders)
                : Intersections.CollidersTouchingComponents(colliders, [Board.Component.Parent]);
            // Not technically placement rules but should work for most things 
            // componentsOnOldBoard = Board.Component.EnumerateChildren()
            //     .Where(addr => Intersections.CollidersTouchingComponents(colliders, [addr]))
            //     .ToArray();
            // Which implementation is better is subjective
            // But I guess this is more consistent if you don't know which board contains the bottom-left corner
            componentsOnOldBoard = Board.Component.EnumerateChildren()
                .Where(splitAlongHorizontal
                    ? address => address.GetComponent().LocalPosition.z < newSize * 0.3f
                    : address => address.GetComponent().LocalPosition.x < newSize * 0.3f
                ).ToArray();
            SetSize(oldSize);
            // DataUpdateInfo.Invoke(Board, []); // shouldn't be necessary because an update is still queued
        }

        private byte[] getDataAtSize(int size)
        {
            SetSize(size);
            var result = Board.SerializeCustomData();
            SetSize(oldSize);
            return result;
        }

        private bool TryCalculateSplit() // returns false if the board is not focused
        {
            var hitInfo = PlayerCaster.CameraCast(Masks.Environment | Masks.Structure | Masks.Peg);
            if (hitInfo.cAddress != Board.Address) return false;

            // Calculate
            var LocalPosition = Board.Component.ToLocalSpace(hitInfo.WorldPoint);
            var Diag = new Vector2Int(
                (int)Math.Floor(LocalPosition.x - LocalPosition.z + 1),
                (int)Math.Floor(LocalPosition.z + LocalPosition.x)
            );
            var prevSAH = splitAlongHorizontal;
            var prevNS = newSize;
            splitAlongHorizontal = Math.Abs(Diag.x + Diag.y) % 2 == 1;
            newSize = splitAlongHorizontal
                ? (Diag.y - Diag.x + 1) / 2
                : (Diag.x + Diag.y) / 2;
            if (prevSAH != splitAlongHorizontal | prevNS != newSize)
                SoundPlayer.PlaySoundGlobal(Sounds.ConnectionInitial);

            oldSize = splitAlongHorizontal ? Board.SizeZ : Board.SizeX;
            drawLine();
            return true;
        }

        private void drawLine()
        {
            if (RenderOutline != null)
                Outliner.RemoveOutline(BoardRender);
            var _Scale = BoardRender.Scale;
            var _Position = BoardRender.WorldPosition;
            BoardRender.Scale = splitAlongHorizontal
                ? BoardRender.Scale with { z = 0.01f }
                : BoardRender.Scale with { x = 0.01f };
            BoardRender.WorldPosition = Board.Component.ToWorldSpace(splitAlongHorizontal
                ? new Vector3(0, 0, newSize)
                : new Vector3(newSize, 0, 0)
            );
            RenderOutline = Outliner.Outline(BoardRender, OutlineData.Valid);
            BoardRender.Scale = _Scale;
            BoardRender.WorldPosition = _Position;
        }
    }

    private static readonly MethodInfo DataUpdateInfo = Methods.getPrivate(typeof(CircuitBoard), "DataUpdate");
}
