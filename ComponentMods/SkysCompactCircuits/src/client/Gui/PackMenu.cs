using System;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using FancyInput;
using JECS.MemoryFiles;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicUI;
using LogicUI.MenuTypes;
using LogicWorld.Audio;
using LogicWorld.Building.Overhaul;
using LogicWorld.GameStates;
using LogicWorld.Interfaces;
using LogicWorld.Modding;
using LogicWorld.SharedCode.Components;
using SkysCompactCircuits.Client.ClientCode;
using SkysCompactCircuits.Client.Keybindings;
using SkysCompactCircuits.Shared;
using SkysCompactCircuits.Shared.Packets;
using SkysGeneralLib.Client.TypeExtensions;
using SkysGeneralLib.Shared.Networking;
using TMPro;
using UnityEngine;

namespace SkysCompactCircuits.Client.Gui;

public partial class PackMenu : ToggleableSingletonMenu<PackMenu>, IAssignMyFields
{
    public ProtoCircuit Circuit;
    public Block[] PrefabBlocks;

    public enum PrefabModes
    {
        Default = 0,
        Board,
        Floor,
        Blank,
        Offset,
        SimpleOffset,
        Custom,
    };
    public PrefabModes PrefabMode;

    public override void Initialize()
    {
        base.Initialize();
        //Setup events and handlers:
        Blocks.onValueChanged.AddListener(_ => UpdateBlocks());
        Size.onValueChanged.AddListener(_ => Render());
        Offset.onValueChanged.AddListener(_ => UpdateOffset());
        UseWidth.OnValueChanged += b =>
        {
            ((TMP_Text)Size.placeholder).text = (b ? Circuit.RootSize.x : Circuit.RootSize.y).ToString();
            Render();
        };
        PrefabDropdown.OnSelectionChange += v => SetupPrefabMode((PrefabModes)v);

        SubmitButton.OnClickBegin += Submit;
    }

    public static string GameStateTextID => "SkysCompactCircuits.PackMenu";
    public static void OpenMenu(ComponentAddress board) => Instance._OpenMenu(board);
    private void _OpenMenu(ComponentAddress board)
    {
        // Hopefully the player notices this is the problem
        // (we will be adding it directly to the hotbar so theres no sense in opening the menu if we can't add it later)
        if (Instances.Hotbar.HotbarIsFull)
        {
            Instances.Hotbar.SelectedSlot = Instances.Hotbar.SelectedSlot == Instances.Hotbar.HotbarItemsCount - 1 ? 0 : Instances.Hotbar.HotbarItemsCount - 1;
            SoundPlayer.PlayFail();
            if (GameStateManager.CurrentStateID == "MHG.ActionWheel")
                GameStateManager.TransitionBackToBuildingState(); // for if you used the build action from the wheel
            return;
        }
        Circuit = new(board);

        ((TMP_Text)Size.placeholder).text = (UseWidth.Value ? Circuit.RootSize.x : Circuit.RootSize.y).ToString();

        // we need to open the menu before rendering the subassembly
        GameStateManager.TransitionTo(GameStateTextID);

        var selected = (PrefabModes)PrefabDropdown.SelectedItemIndex;
        if (PrefabBlocks is null && selected == PrefabModes.Custom)
            SetupPrefabMode(PrefabModes.Default); // should never be called?
        if (selected != PrefabModes.Custom)
            SetupPrefabMode(selected); // will call Render() for us
        else
            UpdateBlocks(); // will call Render() for us
    }

    public void Submit()
    {
        if (PrefabBlocks is null || !(PrefabBlocks.Length != 0 || Circuit?.AddonAddresses?.Length != 0 || Circuit?.ExportPegs?.Length != 0))
        {
            SoundPlayer.PlayFail();
            return;
        }

        // this will create some latency, deal with it
        Instances.SendData.Send(new IndexCircuitRequestPacket() { data = GenerateData().Encode() });
        GameStateManager.TransitionBackToBuildingState();
    }

    static PackMenu() => FuncPacketHandler<IndexCircuitResponsePacket>.Add(packet => IndexAndAddToHotbar(packet.data));
    public static void IndexAndAddToHotbar(byte[] data)
    {
        if (!(PackedCircuitManager.TryDecode(data, () => IndexAndAddToHotbar(data)) is { } circuit))
            return;
        AddToHotbar(circuit);
    }

    public void Render() => Renderer.RenderSubassembly(PreviewPackRender.CreatePreview(GenerateData()));
    public FullPackedCircuitData GenerateData()
    {
        // A bunch of annoying fiddling to turn the ui settings into the required transforms </3
        // (Most of the fiddling has been moved to the inner method but it's no less annoying)
        var mainSize = Size.text.Length > 0 && Size.text[0] != '-' ? int.Parse(Size.text).Clamp(1, 200) : (int?)null;
        var additionalOffset = new Vector3(0, Offset.text.Length > 0 && Offset.text != "-" ? float.Parse(Offset.text) : 0, 0);
        return Circuit.GenerateData(PrefabBlocks, UseWidth.Value, Name.text.Trim(), mainSize, additionalOffset);
    }

    #region Game interactions
    public class PackState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => false;
        public override string TextID => GameStateTextID;
        public override void OnEnter() => ShowMenu();
        public override void OnRun()
        {
            if (CustomInput.AnyDown(
                UITrigger.Back
            ))
                GameStateManager.TransitionBackToBuildingState();
        }
        public override void OnExit()
        {
            HideMenu();
        }
    }
    public class PackOperation : BuildingOperation
    {
        public override InputTrigger OperationStarter => SkysCompactCircuitsTrigger.Pack;
        public override string IconHexCode => "f466";
        public override void BeginOperationOn(ComponentSelection selection)
        {
            if (selection.FirstComponentInSelection.GetClientCode() is PackedCircuit packedCircuit)
                Unpacking.Unpack(packedCircuit);
            else
                OpenMenu(selection.FirstComponentInSelection);
        }

        public override bool CanOperateOn(ComponentSelection selection)
        {
            return selection.Count == 1 && (
                CircuitBoardLikeTypes.IsCircuitBoardLikeInMainWorld(selection.FirstComponentInSelection) ||
                selection.FirstComponentInSelection.GetClientCode() is PackedCircuit
                );
        }
    }
    #endregion

    #region UI interactions
    public void UpdateBlocks()
    {
        if (PrefabMode != PrefabModes.Custom)
            return;
        Block[] blocks;
        try { blocks = new MemoryDataFile(("Blocks:\n" + Blocks.text).Replace("\n", "\n ")).Get<Block[]>("Blocks"); }
        catch (Exception) { return; }

        PrefabBlocks = blocks;
        Render();
    }
    public void UpdateOffset()
    {
        if (PrefabMode != PrefabModes.Offset && PrefabMode != PrefabModes.SimpleOffset)
        {
            Render();
            return;
        }

        var additionalOffset = Offset.text.Length > 0 && Offset.text != "-" ? float.Parse(Offset.text) : 0;
        if (additionalOffset > -0.5f)
        {
            PrefabBlocks = PrefabMode == PrefabModes.Offset
                ? [.. Circuit.AllBoardBlocks]
                : [Circuit.RootBlock];
            PrefabBlocks[0] = new()
            {
                MeshName = PrefabBlocks[0].MeshName,
                Rotation = PrefabBlocks[0].Rotation,
                ShouldBeOutlined = PrefabBlocks[0].ShouldBeOutlined,
                RawColor = PrefabBlocks[0].RawColor,
                Material = PrefabBlocks[0].Material,
                ColliderData = PrefabBlocks[0].ColliderData,
                Position = PrefabBlocks[0].Position + new Vector3(0, -additionalOffset, 0),
                Scale = new(PrefabBlocks[0].Scale.x, 0.5f + additionalOffset, PrefabBlocks[0].Scale.z)
            };
        }
        else
            PrefabBlocks = PrefabMode == PrefabModes.Offset ? Circuit.AllBoardBlocks[1..] : [];

        // PrefabBlocks[0].
        Render();
    }

    public void SetupPrefabMode(PrefabModes mode)
    {
        if (mode == PrefabModes.Custom)
        {
            if (PrefabMode == PrefabModes.Custom) return;
            CustomElement.SetActive(true);
            OffsetElement.SetActive(true);
            Blocks.text = ProtoCircuit.ConvertBlocksToCompactJecs(PrefabBlocks);
        }
        else if (mode == PrefabModes.Offset || mode == PrefabModes.SimpleOffset)
        {
            CustomElement.SetActive(false);
            OffsetElement.SetActive(true);
            if (!Circuit.RootIsCircuitBoard && mode == PrefabModes.Offset)
                mode = PrefabModes.SimpleOffset;

            PrefabMode = mode;
            PrefabDropdown.SetSelectedItemWithoutNotify((int)mode);
            UpdateOffset();
            return;
        }
        else
        {
            Offset.text = (mode == PrefabModes.Floor || mode == PrefabModes.Blank) ? "-0.5" : "";
            CustomElement.SetActive(false);
            OffsetElement.SetActive(false);
            if (!Circuit.RootIsCircuitBoard && (mode == PrefabModes.Default || mode == PrefabModes.Floor))
                mode = PrefabModes.Board;

            PrefabBlocks = mode switch
            {
                PrefabModes.Default => Circuit.AllBoardBlocks,
                PrefabModes.Floor => Circuit.AllBoardBlocks[1..],
                PrefabModes.Board => [Circuit.RootBlock],
                PrefabModes.Blank => [],
                _ => throw new NotImplementedException(),
            };
        }
        PrefabMode = mode;
        PrefabDropdown.SetSelectedItemWithoutNotify((int)mode);
        Render();
    }
    #endregion
    static void AddToHotbar(IPackedCircuitData data)
    {
        var singleComponentHotbarItemData = new DetailedHotbarItemData(
            SkysCompactCircuits_ClientMod.PackedCircuitTextID, data.Encode(),
            inputCount: data.InputCount,
            outputCount: data.OutputCount);
        for (var i = 0; i < Instances.Hotbar.HotbarItemsCount; i++)
            if (Instances.Hotbar.HotbarItemInfo(i) == singleComponentHotbarItemData)
            {
                Instances.Hotbar.SelectedSlot = i;
                return;
            }
        if (Instances.Hotbar.AddItem(singleComponentHotbarItemData)) // will play fail sound on failure
            Instances.Hotbar.SelectedSlot = Instances.Hotbar.HotbarItemsCount - 1;
    }
}
