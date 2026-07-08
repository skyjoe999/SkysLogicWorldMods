using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Building.Placement;
using LogicWorld.Building.Subassemblies;
using LogicWorld.ClientCode;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Components;
using SkysCompactCircuits.Client.Addons;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Client.TypeExtensions;
using UnityEngine;

namespace SkysCompactCircuits.Client.Gui;

public class ProtoCircuit
{
    public SubassemblyData SubassemblyToSave;
    public Vector2Int RootSize;
    public Color24 RootColor;
    public ComponentInput[] ExportPegs;
    public ComponentAddress[] AddonAddresses;
    public Block[] AllBoardBlocks;
    public Block RootBlock;
    public bool RootIsCircuitBoard; // should be, but it might be some other subclass
    public IPackedCircuitData[] InnerDatas;

    public ProtoCircuit(ComponentAddress board)
    {
        var boardData = (board.GetClientCode() as CircuitBoard).Data;
        RootSize = new(boardData.SizeX, boardData.SizeZ);
        RootColor = boardData.Color;
        RootBlock = new() { Scale = new(RootSize.x, 0.5f, RootSize.y), RawColor = RootColor };

        SubassemblyToSave = SubassembliesManager.CreateSubassemblyDataFromSelection(new([board]));

        // The PlacementType part is to account for a bug, when/if it gets fixed, this will then be broken </3 (Just remove it and only check for flipped)
        try { if (PlacementUtilities.GetPlacementData(board) is PlacementData_Standard pd && (pd.Flipped != (pd.PlacementType == PlacementType.OnFixedPlacementPoint))) UnFlipSubassembly(ref SubassemblyToSave, RootSize); }
        catch (Exception) { }

        (SubassemblyToSave.PartialWorldData.OrderedComponentsAndAddresses[0].componentData as IEditableComponentData).LocalPosition = Vector3.zero;
        (SubassemblyToSave.PartialWorldData.OrderedComponentsAndAddresses[0].componentData as IEditableComponentData).LocalRotation = Quaternion.identity;

        var innerCircuitsAndChildren = FindInnerCircuitsAndChildren(SubassemblyToSave.PartialWorldData);
        var boardType = SubassemblyToSave.PartialWorldData.OrderedComponentsAndAddresses[0].componentData.Type;
        RootIsCircuitBoard = SubassemblyToSave.PartialWorldData.ComponentIDsMap[boardType.NumericID] == "MHG.CircuitBoard";

        ExportPegs = SubassemblyToSave.PartialWorldData.ComponentIDsMap.Where(p => p.Value == "SkysCompactCircuits.ExportPeg").Aggregate((ushort?)null, (_, v) => v.Key) is ushort ExportPegID
                ? [.. ClientAddonManager.TransformsFor(SubassemblyToSave.PartialWorldData)
                    .Where(c => c.data.Type.NumericID == ExportPegID)
                    .Where(c => !innerCircuitsAndChildren.Contains(c.address))
                    .Select(c => new ComponentInput() { Position = c.position, Rotation = c.rotation.eulerAngles, Length = 2 / 3f })
                ]
                : [];

        AddonAddresses = [..
            ClientAddonManager.GeneratorsFor(SubassemblyToSave.PartialWorldData)
                .Select(g => g.Address)
                .Where(address => !innerCircuitsAndChildren.Contains(address))
            ];

        AllBoardBlocks = RootIsCircuitBoard ? [.. GetBoardBlocks()] : [];

        IEnumerable<Block> GetBoardBlocks()
        {
            var rootOffset = new Vector3(RootSize.x, 0, RootSize.y) / 2;
            var manager = new CustomDataManager<CircuitBoard.IData>();
            return ClientAddonManager.TransformsFor(SubassemblyToSave.PartialWorldData)
                .Where(g => g.data.Type == boardType)
                .Where(g => !innerCircuitsAndChildren.Contains(g.address))
                .Select(d => ForBoard(d.data, d.position, d.rotation));
            Block ForBoard(ComponentData data, Vector3 pos, Quaternion rot) => manager.TryDeserializeData(data.CustomData)
                    ? new() { Scale = new(manager.Data.SizeX, 0.5f, manager.Data.SizeZ), RawColor = manager.Data.Color, Position = pos + rot * new Vector3(manager.Data.SizeX, 0, manager.Data.SizeZ) / 2f - rootOffset, Rotation = rot.eulerAngles }
                    : new() { Scale = new(1, 0.5f, 1), RawColor = new(120, 120, 120), Position = pos, Rotation = rot.eulerAngles };
        }
    }

    public FullPackedCircuitData GenerateData(Block[] blocks, bool useWidth, string name, int? mainSize = null, Vector3 additionalOffset = default)
    {
        // A bunch of annoying fiddling to turn create the required transforms </3
        mainSize ??= (useWidth ? RootSize.x : RootSize.y);
        var scale = (float)mainSize / (useWidth ? RootSize.x : RootSize.y);
        var minorSizeF = (useWidth ? RootSize.y : RootSize.x) * scale;
        var minorSize = Mathf.CeilToInt(minorSizeF - 0.001f).Clamp(1, int.MaxValue);
        var size = useWidth
            ? new Vector2Int(mainSize.Value, minorSize)
            : new Vector2Int(minorSize, mainSize.Value);

        // this is such a mess TT
        var addonOffset = (minorSizeF - minorSize) / -2 * (useWidth ? Vector3.forward : Vector3.right);
        var blockOffset = (new Vector3(-0.5f, 0, -0.5f) + new Vector3(size.x, 0, size.y) / 2) / scale;
        var inputOffset = (new Vector3(-0.5f, 0, -0.5f) + addonOffset) / scale;

        var componentPrefab = new Prefab { Blocks = blocks }.Transform(position: blockOffset + additionalOffset)
                .Join(new Prefab { Inputs = ExportPegs }.Transform(position: inputOffset + additionalOffset));

        return new()
        {
            ComponentPrefab = componentPrefab,
            PartialWorld = SubassemblyToSave.PartialWorldData,
            Size = size,
            TransformOffset = new Vector3(-0.5f, 0, -0.5f) + addonOffset + additionalOffset * scale,
            TransformScale = scale,
            AddonAddresses = AddonAddresses,
            Name = name,
        };
    }

    public static HashSet<ComponentAddress> FindInnerCircuitsAndChildren(PartialWorldData world)
    {
        if (world.ComponentIDsMap.Where(p => p.Value == "SkysCompactCircuits.PackedCircuit").Aggregate((ushort?)null, (_, v) => v.Key) is not ushort CircuitID)
            return [];

        var innerAddresses = new HashSet<ComponentAddress>();
        foreach (var (address, componentData) in world.OrderedComponentsAndAddresses)
            if (componentData.Type.NumericID == CircuitID || innerAddresses.Contains(componentData.Parent))
                innerAddresses.Add(address);

        return innerAddresses;
    }

    public static void UnFlipSubassembly(ref SubassemblyData subassembly, Vector2Int rootSize)
    {
        var root = subassembly.PartialWorldData.OrderedComponentsAndAddresses[0].address;

        var rotation = Quaternion.AngleAxis(180, Vector3.forward);
        var pivot = new Vector3Int(rootSize.x * 500, 250, rootSize.y * 500) * 3 / 10;
        var scaleRotation = new Vector3Int(-1, -1, 1);
        foreach (var ((address, data), i) in subassembly.PartialWorldData.OrderedComponentsAndAddresses.Select((p, i) => (p, i)))
            if (data.Parent == root) Fix(data);
        void Fix(IEditableComponentData data)
        {
            data.LocalRotation = rotation * data.LocalRotation;
            data.LocalPositionFixed = scaleRotation * (data.LocalPositionFixed - pivot) + pivot;
        }
    }

    // 50+ lines of code just to make nice(-ish!) looking text... </3
    public static string ConvertBlocksToCompactJecs(Block[] blocks)
    {
        if (blocks is null || blocks.Length == 0)
            return "";
        var template = Block.Standard;
        var builder = new StringBuilder();
        foreach (var block in blocks)
        {
            builder.Append("-\n");
            var len = builder.Length;
            if (block.Position != template.Position)
                builder.Append($" position: {block.Position}\n");
            if (block.Rotation != template.Rotation)
                builder.Append($" rotation: {block.Rotation}\n");
            if (block.Scale != template.Scale)
                builder.Append($" scale: {block.Scale}\n");
            if (block.ShouldBeOutlined != template.ShouldBeOutlined)
                builder.Append($" shouldBeOutlined: {block.ShouldBeOutlined}\n");
            if (block.RawColor != template.RawColor)
                builder.Append($" color: {block.RawColor.ToStringWithoutPrefix()}\n");
            if (block.Material != template.Material)
                builder.Append($" material: {block.Material}\n");
            if (block.MeshName != template.MeshName)
                builder.Append($" mesh: {block.MeshName}\n");
            var transformDifferent =
                block.ColliderData.Transform.LocalPosition != template.ColliderData.Transform.LocalPosition ||
                block.ColliderData.Transform.LocalRotation != template.ColliderData.Transform.LocalRotation ||
                block.ColliderData.Transform.LocalScale != template.ColliderData.Transform.LocalScale;
            if (
                block.ColliderData.Layer != template.ColliderData.Layer ||
                block.ColliderData.Type != template.ColliderData.Type ||
                transformDifferent
            )
            {
                builder.Append(" colliderData:\n");
                if (block.ColliderData.Type != template.ColliderData.Type)
                    builder.Append($"  type: {block.ColliderData.Type}\n");
                if (block.ColliderData.Layer != template.ColliderData.Layer)
                    builder.Append($"  layer: {block.ColliderData.Layer}\n");
                if (transformDifferent)
                {
                    builder.Append("  transform:\n");
                    if (block.ColliderData.Transform.LocalPosition != template.ColliderData.Transform.LocalPosition)
                        builder.Append($"   position: {block.ColliderData.Transform.LocalPosition}\n");
                    if (block.ColliderData.Transform.LocalRotation != template.ColliderData.Transform.LocalRotation)
                        builder.Append($"   rotation: {block.ColliderData.Transform.LocalRotation}\n");
                    if (block.ColliderData.Transform.LocalRotation != template.ColliderData.Transform.LocalRotation)
                        builder.Append($"   scale: {block.ColliderData.Transform.LocalRotation}\n");
                }
            }
            if (len == builder.Length)
            {
                builder.RemoveCharactersAtEnd(1);
                builder.Append(" Standard\n");
            }
        }
        builder.RemoveCharactersAtEnd(1);
        return builder.ToString();
    }
}
