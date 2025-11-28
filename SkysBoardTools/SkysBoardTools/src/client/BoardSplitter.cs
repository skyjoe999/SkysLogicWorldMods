using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EccsLogicWorldAPI.Shared.AccessHelper;
using FancyInput;
using LogicAPI.Data;
using LogicAPI.Data.BuildingRequests;
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
using LogicWorld.BuildingManagement;
using SkysGeneralLib.Shared.AccessTools;
using LogicLog;

namespace SkysBoardTools.Client;

public static class BoardSplitter
{
    public static string GameStateTextID => "SkysBoardTools.BoardSplitting";

    public static readonly HashSet<ComponentType> UnSplittableTypes = []; // Modding considerations? In LW? Unheard-of

    public static SplitManager Splitting;

    public class SplitBoardsOperation : BuildingOperation
    {
        public override InputTrigger OperationStarter => SkysBoardToolsTrigger.SplitBoards;
        public override string IconHexCode => "e49c";

        public override bool CanOperateOn(ComponentSelection selection)
        {
            if (selection.Count != 1) return false;

            return selection.FirstComponentInSelection.GetClientCode() is CircuitBoard clientCode &&
                !UnSplittableTypes.Contains(clientCode.Component.Data.Type);
        }

        public override void BeginOperationOn(ComponentSelection selection)
        {
            Splitting = new(selection.FirstComponentInSelection.GetClientCode() as CircuitBoard);
            GameStateManager.TransitionTo(GameStateTextID);
        }
    }

    private class SplitBoardsState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => true;

        public override bool ShowHotbarWhileStateActive => false;

        public override string TextID => GameStateTextID;

        public override void OnRun() => Splitting.OnRun();

        public override void OnExit()
        {
            Splitting?.Dispose();
            Splitting = null;
        }

        public override IEnumerable<InputTrigger> HelpScreenTriggers => [];
    }

    public class SplitManager : IDisposable
    {
        private readonly CircuitBoard Board;
        private readonly IRenderedEntity BoardRender;

        private Outline RenderOutline;

        public bool IsValid => oldSize != newSize & 0 != newSize;

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


        public void Dispose() => RemoveLine();

        public void OnRun()
        {
            // Cancel
            if (CustomInput.AnyDown(
                    Trigger.Delete,
                    UITrigger.Back,
                    Trigger.CancelPlacing,
                    Trigger.Undo,
                    Trigger.Redo
                ))
                Cancel();
            // Finish
            if (CustomInput.AnyDown(
                    Trigger.Place,
                    SkysBoardToolsTrigger.SplitBoards,
                    SkysBoardToolsTrigger.SplitOrJoinBoards
                ))
                if (!TryFinish())
                    Cancel();
            // Update
            // I have decided to leave the line to make it clearer that the tool is still active
            TryCalculateSplit();
        }

        private void Cancel()
        {
            // Let dispose handle the rest
            SoundPlayer.PlaySoundGlobal(Sounds.DeleteSomething);
            GameStateManager.TransitionBackToBuildingState();
        }

        public bool TryFinish()
        {
            if (!IsValid)
                return false;
            Finish();
            return true;
        }

        private void Finish()
        {
            DecideParent();

            var componentsOnNewBoard = Board.Component.EnumerateChildren()
                .Except(componentsOnOldBoard).ToArray();
            if (!keepAsRoot)
                (componentsOnOldBoard, componentsOnNewBoard) = (componentsOnNewBoard, componentsOnOldBoard);
            int fullSize = splitAlongHorizontal ? Board.SizeZ : Board.SizeX;
            var localOffset = splitAlongHorizontal
                ? Vector3.zero with { z = newSize }
                : Vector3.zero with { x = newSize };

            var newData = (IEditableComponentData)Board.Component.Data.Duplicate();
            newData.CustomData = GetDataAtSize(keepAsRoot ? fullSize - newSize : newSize);
            newData.LocalPosition = keepAsRoot ? localOffset * 0.3f : -localOffset * 0.3f;
            newData.LocalRotation = Quaternion.identity;
            newData.Parent = Board.Address;

            // Set the size of the original board
            List<BuildRequest> requests = [
                new BuildRequest_UpdateComponentCustomData(
                    Board.Address,
                    GetDataAtSize(keepAsRoot ? newSize : fullSize - newSize)
                ),
            ];
            if (!keepAsRoot)
                // Move the old board into place
                requests.Add(new BuildRequest_RepositionComponent(
                    Board.Address,
                    Board.Component.LocalRotation * localOffset * 0.3f
                ));

            // Add the new board
            requests.Add(new BuildRequest_CreateSingleNewComponent((ComponentData)newData));

            requests.SendAllRequests(receipt =>
            {
                if (componentsOnNewBoard.Length == 0 || !receipt.ActionSuccessfullyApplied)
                    return;
                if (receipt.RequestsToUndo.Last() is not BuildRequest_RemoveComponentsAndChildrenAndAttachedWires { TargetAddresses: { } newAddresses })
                {
                    LogicLogger.For("Board Splitter").Error("Could not find address of new board");
                    SoundPlayer.PlayFail();
                    UndoManager.Undo();
                    return;
                }
                var newAddress = newAddresses.First();
                requests = [
                    // Re-parent the components on the new board onto the new board
                    ..componentsOnNewBoard.Select(addr =>
                        new BuildRequest_UpdateComponentPositionRotationParent(
                            addr,
                            addr.GetComponent().LocalPosition - (keepAsRoot ? localOffset * 0.3f : Vector3.zero),
                            addr.GetComponent().LocalRotation,
                            newAddress
                        ))
                ];
                requests.SendAllRequests(_ =>
                {
                    // merges the last two undo actions
                    if (componentsOnNewBoard.Length == 1)
                        (UndoHistory[^1], UndoHistory[^2]) = (UndoHistory[^2], UndoHistory[^1]);
                    else
                    {
                        var index = UndoHistory.Select((v, i) => (v, i)).Reverse().First(p => p.v is MultiUndoMarker_Start).i;
                        UndoHistory[index] = UndoRequests.CreateEmpty();
                        UndoHistory[index - 1] = UndoRequests.CreateEmpty();
                    }
                });
            });
            GameStateManager.TransitionBackToBuildingState();
        }

        private void SetSize(int size)
        {
            if (splitAlongHorizontal) Board.SizeZ = size;
            else Board.SizeX = size;
        }

        private void DecideParent()
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
            componentsOnOldBoard = [.. Board.Component.EnumerateChildren()
                .Where(splitAlongHorizontal
                    ? address => address.GetComponent().LocalPosition.z < newSize * 0.3f
                    : address => address.GetComponent().LocalPosition.x < newSize * 0.3f
                )];
            SetSize(oldSize);
            // DataUpdateInfo.Invoke(Board, []); // shouldn't be necessary because an update is still queued
        }

        private byte[] GetDataAtSize(int size)
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
            DrawLine();
            return true;
        }

        private void DrawLine()
        {
            RemoveLine();
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

        public void RemoveLine()
        {
            if (RenderOutline is not null)
                Outliner.RemoveOutline(BoardRender);
        }
    }
    private static readonly List<UndoHistoryItem> UndoHistory = new StaticAccessor<List<UndoHistoryItem>>(typeof(UndoManager), "UndoHistory").Get();
    private static readonly MethodInfo DataUpdateInfo = Methods.getPrivate(typeof(CircuitBoard), "DataUpdate");
}
