using System;
using System.Collections.Generic;
using System.Linq;
using FancyInput;
using JetBrains.Annotations;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Data.BuildingRequests;
using LogicUI;
using LogicWorld.Audio;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.GameStates;
using LogicWorld.Input;
using LogicWorld.Outlines;
using LogicWorld.Physics;
using LogicWorld.Players;
using SkysGeneralLib.Client.TypeExtensions;
using SkysGeneralLib.Client.BuildRequests;
using SkysBoardTools.Client.Keybindings;
using UnityEngine;

namespace SkysBoardTools.client;

public static class BoardJoiner
{
    public static string GameStateTextID => "SkysBoardTools.BoardJoining";

    // TODO: Help screen
    private static IEnumerable<InputTrigger> _HelpScreenTriggers() => [];
    private static readonly List<ComponentType> UnJoinableTypes = new(); // Modding considerations? In LW? Unheard-of
    public static JoiningManager joining;

    public static void AddUnJoinableType(ComponentType type)
    {
        if (!UnJoinableTypes
                .Contains(type)) // should always be true but might save someone a headache at a one-time cost
            UnJoinableTypes.Add(type);
    }

    [UsedImplicitly]
    public class JoinBoardsOperation : BuildingOperation
    {
        public override InputTrigger OperationStarter => SkysBoardToolsTrigger.JoinBoards;


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
                if (UnJoinableTypes.Contains(type)) continue;
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
                if (UnJoinableTypes.Contains(type)) continue;
                GameStateManager.TransitionTo(GameStateTextID);
                joining = new JoiningManager(clientCode);
                return;
            }
            SoundPlayer.PlayFail();
        }
    }

    [UsedImplicitly]
    private class JoinBoardsState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => true;

        public override bool ShowHotbarWhileStateActive => false;

        public override string TextID => GameStateTextID;

        public override void OnRun() => joining.OnRun();

        public override void OnExit()
        {
            joining.Dispose();
            joining = null;
        }

        public override IEnumerable<InputTrigger> HelpScreenTriggers => _HelpScreenTriggers();
    }

    public class JoiningManager : IDisposable
    {
        private CircuitBoard MainBoard;
        private CircuitBoard OtherBoard;
        private bool areFlipped;
        private bool isValid;
        private bool transposeSize; // Swap SizeX and SizeZ for the other board
        private Vector2 correctedPosition;
        private bool mergeVertical;

        public JoiningManager(CircuitBoard mainBoard)
        {
            MainBoard = mainBoard;
            Outliner.Outline(mainBoard.Address, OutlineData.LookingAtComponent);
        }

        public void Dispose()
        {
            Outliner.RemoveOutline(MainBoard.Address);
            if (OtherBoard != null)
                Outliner.RemoveOutline(OtherBoard.Address);
        }

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
            // TODO: if join pressed select new board on success and other board on failure?
            if (CustomInput.AnyDown(
                    Trigger.Place,
                    SkysBoardToolsTrigger.JoinBoards
                ))
                if (isValid)
                    Finish();

            // Update
            if (!TrySetOtherBoard() && OtherBoard != null)
            {
                Outliner.RemoveOutline(OtherBoard.Address);
                isValid = false; // maybe that fixes the crash? idk
                OtherBoard = null;
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
            // Might need to swap if MainBoard is a descendant of OtherBoard
            // TODO: swap with reparenting, this only works if the board are fungible
            if (MainBoard.Address.DescendsFrom(OtherBoard.Address))
            {
                (MainBoard, OtherBoard) = (OtherBoard, MainBoard);
                _ = CheckIsValid(); // need to set our globals
            }

            // Does the main board need to move
            var HShift = Math.Abs(correctedPosition.x) > 0.001 && correctedPosition.x < 0;
            var VShift = Math.Abs(correctedPosition.x) < 0.001 && correctedPosition.y < 0;

            List<BuildRequest> requests = [];
            var shiftAmount =
                new Vector3(HShift ? correctedPosition.x : 0, 0f, VShift ? correctedPosition.y : 0) * 0.3f;
            if (HShift || VShift)
                requests.Add(new BuildRequest_RepositionComponent(
                    MainBoard.Address,
                    MainBoard.Component.LocalRotation * shiftAmount
                ));

            requests.Add(SetSizeRequest());

            // really hoping these execute in order
            ComponentAddress newParent = MainBoard.Address;
            requests.AddRange(OtherBoard.Component.EnumerateChildren().Select(targetAddress =>
                new BuildRequest_UpdateComponentPositionRotationParent(
                    targetAddress,
                    newParent.GetComponent()
                        .ToLocalSpace(targetAddress.GetComponent().WorldPosition) * 0.3f - shiftAmount,
                    newParent.GetComponent().WorldRotation.Inverse() * targetAddress.GetComponent().WorldRotation,
                    newParent
                )
            ));
            requests.Add(new BuildRequest_RemoveComponentsAndChildrenAndAttachedWires([OtherBoard.Address]));
            requests.SendAllRequests();
            
            SoundPlayer.PlaySoundGlobal(Sounds.PlaceOnBoard);
            GameStateManager.TransitionBackToBuildingState();
        }

        private BuildRequest_UpdateComponentCustomData SetSizeRequest()
        {
            byte[] data;
            if (mergeVertical)
            {
                MainBoard.SizeZ += transposeSize ? OtherBoard.SizeX : OtherBoard.SizeZ;
                data = MainBoard.SerializeCustomData();
                MainBoard.SizeZ -= transposeSize ? OtherBoard.SizeX : OtherBoard.SizeZ;
            }
            else
            {
                MainBoard.SizeX += transposeSize ? OtherBoard.SizeZ : OtherBoard.SizeX;
                data = MainBoard.SerializeCustomData();
                MainBoard.SizeX -= transposeSize ? OtherBoard.SizeZ : OtherBoard.SizeX;
            }

            return new BuildRequest_UpdateComponentCustomData(MainBoard.Address, data);
        }

        private bool TrySetOtherBoard() // Returns true if a possible board was found (even if not new)
        {
            // not resetting is valid here is crashing the game
            var info = PlayerCaster.CameraCast(Masks.Environment | Masks.Structure | Masks.Peg | Masks.PlayerModel);
            if (!info.HitComponent) return false;
            if (info.cAddress == MainBoard.Address) return false;
            if (info.cAddress == OtherBoard?.Address) return true;
            if (info.cAddress.GetClientCode() is not CircuitBoard NewBoard)
                return false;
            if (UnJoinableTypes.Contains(NewBoard.Component.Data.Type)) return false;
            SetOtherBoard(NewBoard);
            return true;
        }

        private void SetOtherBoard(CircuitBoard NewBoard)
        {
            if (OtherBoard != null)
                Outliner.RemoveOutline(OtherBoard.Address);
            OtherBoard = NewBoard;
            isValid = CheckIsValid(); // I could do this in the next line but every IDE ever will assume I meant ==
            SoundPlayer.PlaySoundGlobal(Sounds.ConnectionInitial);
            Outliner.Outline(OtherBoard.Address, isValid ? OutlineData.Valid : OutlineData.Invalid);
        }

        private bool CheckIsValid()
        {
            if (!MainBoard.Component.up.IsPrettyCloseToBeingParallelWith(OtherBoard.Component.up)) return false;
            // Alright! Flipping only flips along the x apparently! (not x and z)
            areFlipped = !OtherBoard.Component.up.IsPrettyCloseToPointingInTheSameDirectionAs(MainBoard.Component.up);
            var LocalPosition = MainBoard.Component.ToLocalSpace(
                areFlipped
                    ? OtherBoard.Component.WorldPosition + 0.3f * (
                        OtherBoard.Component.up * 0.5f +
                        OtherBoard.Component.right * OtherBoard.SizeX
                    )
                    : OtherBoard.Component.WorldPosition);
            if (Math.Abs(LocalPosition.y) > 0.001f) return false; // not same height
            var localRight = (
                MainBoard.Component.ToLocalSpace(
                    OtherBoard.Component.right * (areFlipped ? -1 : 1)
                    + MainBoard.Component.WorldPosition)
            ).normalized; // There has to be a nicer way but linear is too much work
            if (Math.Abs(Math.Abs(localRight.x) + Math.Abs(localRight.z) - 1) > 0.001f)
                return false; // non-90*n rotation

            // We know the boards are coplanar and we have accounted for flipping
            // Now we correct for rotation
            transposeSize = Math.Abs(localRight.z) > 0.5f;
            correctedPosition = new Vector2(
                LocalPosition.x - (transposeSize
                    ? (localRight.z > 0 ? OtherBoard.SizeZ : 0) // was 90ccw
                    : (localRight.x < 0 ? OtherBoard.SizeX : 0)), // was 180
                LocalPosition.z - (transposeSize
                    ? (localRight.z < 0 ? OtherBoard.SizeX : 0) // was 90cw
                    : (localRight.x < 0 ? OtherBoard.SizeZ : 0)) // was 180
            );
            mergeVertical = Math.Abs(correctedPosition.x) < 0.001;
            if (!mergeVertical) // Horizontally offset
            {
                if (Math.Abs(correctedPosition.y) > 0.001) return false; // Diagonal
                if (MainBoard.SizeZ != (transposeSize ? OtherBoard.SizeX : OtherBoard.SizeZ))
                    return false; // Different side lengths
                return correctedPosition.x > 0
                    ? Math.Abs(MainBoard.SizeX - correctedPosition.x) < 0.001
                    : Math.Abs((transposeSize ? OtherBoard.SizeZ : OtherBoard.SizeX) + correctedPosition.x) < 0.001;
            }

            // I'm choosing to ignore the possibility correctedPosition = 0, 0
            if (MainBoard.SizeX != (transposeSize ? OtherBoard.SizeZ : OtherBoard.SizeX))
                return false; // Different side lengths
            return correctedPosition.y > 0
                ? Math.Abs(MainBoard.SizeZ - correctedPosition.y) < 0.001
                : Math.Abs((transposeSize ? OtherBoard.SizeX : OtherBoard.SizeZ) + correctedPosition.y) < 0.001;
        }
    }
}
