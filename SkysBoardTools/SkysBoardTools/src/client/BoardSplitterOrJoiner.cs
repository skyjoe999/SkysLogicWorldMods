using System.Collections.Generic;
using FancyInput;
using LogicUI;
using LogicWorld.Audio;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.GameStates;
using LogicWorld.Input;
using SkysGeneralLib.Client.TypeExtensions;
using SkysBoardTools.Client.Keybindings;

namespace SkysBoardTools.Client;

public static class BoardSplitterOrJoiner
{
    public static string GameStateTextID => "SkysBoardTools.BoardSplittingOrJoining";

    private static ref BoardJoiner.JoiningManager Joining => ref BoardJoiner.Joining;
    private static ref BoardSplitter.SplitManager Splitting => ref BoardSplitter.Splitting;

    public class SplitOrJoinBoardsOperation : BuildingOperation
    {
        public override InputTrigger OperationStarter => SkysBoardToolsTrigger.SplitOrJoinBoards;

        public override bool CanOperateOn(ComponentSelection selection)
        {
            if (selection.Count != 1) return false;
            return selection.FirstComponentInSelection.GetClientCode() is CircuitBoard clientCode && (
                !BoardSplitter.UnSplittableTypes.Contains(clientCode.Component.Data.Type) ||
                !BoardJoiner.UnJoinableTypes.Contains(clientCode.Component.Data.Type)
            );
        }

        public override void BeginOperationOn(ComponentSelection selection)
        {
            var clientCode = selection.FirstComponentInSelection.GetClientCode() as CircuitBoard;

            // If we have a partial match then fall back to one of the other states for simplicity
            if (BoardSplitter.UnSplittableTypes.Contains(clientCode.Component.Data.Type))
            {
                Joining = new(clientCode);
                GameStateManager.TransitionTo(BoardJoiner.GameStateTextID);
            }
            else if (BoardJoiner.UnJoinableTypes.Contains(clientCode.Component.Data.Type))
            {
                Splitting = new(clientCode);
                GameStateManager.TransitionTo(BoardSplitter.GameStateTextID);
            }
            else
            {
                Joining = new(clientCode);
                Splitting = new(clientCode);
                GameStateManager.TransitionTo(GameStateTextID);
            }
        }
    }

    private class SplitOrJoinBoardsState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => true;

        public override bool ShowHotbarWhileStateActive => false;

        public override string TextID => GameStateTextID;

        public override void OnRun()
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
                    SkysBoardToolsTrigger.JoinBoards,
                    SkysBoardToolsTrigger.SplitOrJoinBoards
                ))
            {
                if (!Splitting.TryFinish() && !Joining.TryFinish())
                    Cancel();
            }
            // Update
            // This will call the input code multiple times but that's fine
            // Will be null if we have finished (or canceled)
            Splitting?.OnRun();
            if (!(Splitting?.IsValid ?? true))
                Splitting?.RemoveLine();
            Joining?.OnRun();
        }

        private static void Cancel()
        {
            // Let dispose handle the rest
            SoundPlayer.PlaySoundGlobal(Sounds.DeleteSomething);
            GameStateManager.TransitionBackToBuildingState();
        }

        public override void OnExit()
        {
            Joining?.Dispose();
            Joining = null;
            Splitting?.Dispose();
            Splitting = null;
        }

        public override IEnumerable<InputTrigger> HelpScreenTriggers => [];
    }
}
