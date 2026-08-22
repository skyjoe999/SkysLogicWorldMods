using System;
using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicWorld.SharedCode.Components;
using LogicWorld.SharedCode.PartialWorlds;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Server;
using SkysGeneralLib.Shared;
using UnityEngine;

namespace SkysCompactCircuits.Server;

public static class PackedCircuitStructureManager
{
    public static readonly Dictionary<string, bool> ExportPegIDIsOutput = new() { ["SkysCompactCircuits.ExportPeg"] = false, ["SkysCompactCircuits.ExportThroughPeg"] = false, ["SkysCompactCircuits.ExportThroughBuffer"] = true, ["SkysCompactCircuits.ExportBuffer"] = true };

    public static readonly HashSet<string> TextIDsToExclude = [];
    public static void RegisterExcludedType(string textID) => TextIDsToExclude.Add(textID);

    // This one is session only, saving this data to disk is more trouble than it's worth
    // (Might need to change to reduce startup lag...)
    public static readonly Dictionary<int, PackedCircuitStructure?> StructuresByIndex = [];
    public static readonly Dictionary<Guid, PartialWorldData> ExtraWorldsByGuid = [];

    public static PackedCircuitStructure? GenerateStructureWithCache(IndexedPackedCircuitData circuit)
    {
        if (StructuresByIndex.TryGetValue(circuit.Index, out var structure))
            return structure;
        var world = ModifiedIndexResolver.ExpandModified(circuit);
        return StructuresByIndex[circuit.Index] = GenerateStructure(world, new IndexedPackedCircuitData(circuit.Index).Encode(), [.. circuit.AddonAddresses ?? []]);
    }

    public static PackedCircuitStructure? GenerateStructure(PartialWorldData world, byte[] rootData, HashSet<ComponentAddress> forceInclude = null)
    {
        if (!ActuallyConvert(ref world, Services.ComponentTypesManager))
            return null;


        var inverseWorldIDMap = world.ComponentIDsMap.ToDictionary(p => p.Value, p => p.Key);


        var localCircuitID = inverseWorldIDMap.TryGetValue("SkysCompactCircuits.PackedCircuit", out var value) ? value : (ushort?)null;
        var exportPegIDIsOutput = ExportPegIDIsOutput
            .Where(kvp => inverseWorldIDMap.ContainsKey(kvp.Key))
            .ToDictionary(kvp => inverseWorldIDMap[kvp.Key], kvp => kvp.Value);
        var exportAddresses = FindRootExportPegs(world.OrderedComponentsAndAddresses, exportPegIDIsOutput, localCircuitID).ToArray();

        var includedComponents = FilterForRelevantComponents(world, [.. forceInclude ?? [], .. exportAddresses.Select(peg => peg.ComponentAddress)]).ToList();

        var wires = GenerateWires(includedComponents, world).ToList();

        var onStates = includedComponents
            .SelectMany(p => p.componentData.OutputInfos)
            .Select(o => o.StateID)
            .ToHashSet();
        onStates.IntersectWith(world.OnStates);


        var componentIDsMap = world.ComponentIDsMap.ToDictionary(p => p.Key, p => p.Value); // clone the dictionary
        includedComponents[0] = (
            includedComponents[0].address,
            new EditableComponentData(new ComponentType(localCircuitID ?? (ushort)(componentIDsMap.Keys.Max() + 1)))
            {
                InputInfos = new InputInfo[exportAddresses.Count(export => export.IsInputAddress())],
                OutputInfos = new OutputInfo[exportAddresses.Count(export => export.IsOutputAddress())],
                LocalRotation = Quaternion.identity,
                CustomData = rootData
            }
        );

        if (localCircuitID is null)
            componentIDsMap[includedComponents[0].componentData.Type.NumericID] = "SkysCompactCircuits.PackedCircuit";

        return new()
        {
            AdditionWorld = PartialWorldUtilities.ConvertComponentTypes(new(
                componentIDsMap,
                includedComponents,
                wires,
                onStates
            ), Services.ComponentTypesManager),
            ExportAddresses = exportAddresses,
            OriginalChildAddresses = [.. includedComponents.Select(p => p.address)],
            UnpackingWorld = world,
        };
    }

    public static IEnumerable<(ComponentAddress address, ComponentData componentData)> FilterForRelevantComponents(PartialWorldData world, HashSet<ComponentAddress> alwaysInclude) =>
        FilterForRelevantComponents(world.OrderedComponentsAndAddresses, world.ComponentIDsMap, alwaysInclude);
    public static IEnumerable<(ComponentAddress address, ComponentData componentData)> FilterForRelevantComponents(IReadOnlyList<(ComponentAddress address, ComponentData componentData)> orderedComponentsAndAddresses, IReadOnlyDictionary<ushort, string> componentIDsMap, HashSet<ComponentAddress> alwaysInclude)
    {
        var allTypes = componentIDsMap?.ToDictionary(p => p.Value, p => p.Key);
        var excludedTypes = TextIDsToExclude.Intersect(allTypes.Keys).Select(id => allTypes[id]).ToHashSet();

        // include any component with children to maintain hierarchy (not strictly necessary but might be important for some mods (like mine))
        var componentIndices = orderedComponentsAndAddresses.Select((p, i) => (p.address, (index: i, data: p.componentData))).ToDictionary();
        var keep = new bool[orderedComponentsAndAddresses.Count];

        foreach (var ((address, data), index) in orderedComponentsAndAddresses.Select((v, i) => (v, i)))
            if (!excludedTypes.Contains(data.Type.NumericID) || alwaysInclude.Contains(address))
            {
                keep[index] = true;
                var current = data;
                while (current.Parent != default)
                {
                    var (parentIndex, parentData) = componentIndices[current.Parent];
                    if (keep[parentIndex])
                        break;
                    keep[parentIndex] = true;
                    current = parentData;
                }
            }
        keep[0] = true; // will only be false if everything is false and we dont want to return an empty array
        return orderedComponentsAndAddresses.Where((_, i) => keep[i]);
    }

    public static IEnumerable<PegAddress> FindRootExportPegs(IEnumerable<(ComponentAddress address, ComponentData componentData)> orderedComponentsAndAddresses, Dictionary<ushort, bool> exportPegIDIsOutput, ushort? circuitID = null)
    {
        if (exportPegIDIsOutput.Count == 0)
            yield break;
        if (circuitID is null)
        {
            foreach (var (address, componentData) in orderedComponentsAndAddresses)
                if (exportPegIDIsOutput.TryGetValue(componentData.Type.NumericID, out var isOutput))
                    yield return new(address, 0, isOutput ? PegType.Output : PegType.Input);
        }
        else
        {
            var innerAddresses = new HashSet<ComponentAddress>();
            foreach (var (address, componentData) in orderedComponentsAndAddresses)
                if (componentData.Type.NumericID == circuitID || innerAddresses.Contains(componentData.Parent))
                    innerAddresses.Add(address);
                else if (exportPegIDIsOutput.TryGetValue(componentData.Type.NumericID, out var isOutput))
                    yield return new(address, 0, isOutput ? PegType.Output : PegType.Input);
        }
    }

    public static IEnumerable<Wire> GenerateWires(List<(ComponentAddress address, ComponentData componentData)> includedComponents, PartialWorldData world) =>
        GenerateWires(includedComponents, world.AllWires, world.OrderedComponentsAndAddresses);
    public static IEnumerable<Wire> GenerateWires(List<(ComponentAddress address, ComponentData componentData)> includedComponents, IEnumerable<Wire> wires, IReadOnlyList<(ComponentAddress address, ComponentData componentData)> allComponents)
    {
        var includedAddresses = includedComponents.Select(p => p.address).ToHashSet();

        var stateIDToInputPegs = includedComponents
            .SelectMany(p => p.componentData.InputInfos.Select((input, i) => (id: input.StateID, address: new PegAddress(p.address, i, PegType.Input))))
            .GroupBy(i => i.id, i => i.address)
            .ToDictionary(g => g.Key, g => g.ToHashSet());

        var inputPegsToStates = allComponents
            .SelectMany(p => p.componentData.InputInfos.Select((info, index) => (p.address, index, info.StateID)))
            .ToDictionary(d => new PegAddress(d.address, d.index, PegType.Input), d => d.StateID);

        var inputStatesToOutputWires = wires
            .Where(w => w.Point1.IsOutputAddress() || w.Point2.IsOutputAddress())
            .GroupBy(w => inputPegsToStates[w.Point1.IsOutputAddress() ? w.Point2 : w.Point1])
            .ToDictionary(g => g.Key, g => g.Select(w => (w.Point1.IsOutputAddress() ? w.Point1 : w.Point2, w.StateID)).ToList());

        foreach (var (stateID, inputs) in stateIDToInputPegs)
        {
            PegAddress? prev = null;
            foreach (var input in inputs)
            {
                // chain the inputs together
                if (prev.HasValue)
                    yield return new(prev.Value, input, stateID, 0);
                prev = input;
            }

            foreach (var (output, outputID) in inputStatesToOutputWires.GetValueOrDefault(stateID) ?? [])
                if (includedAddresses.Contains(output.ComponentAddress))
                    yield return new(prev.Value, output, outputID, 0);
        }
    }

    public static bool TryGetAdditionWorld(Guid guid, out PartialWorldData additionWorld) => ExtraWorldsByGuid.TryGetValue(guid, out additionWorld);
    public static bool TryGetAdditionWorldGuid(IndexedPackedCircuitData circuit, out Guid guid)
    {
        guid = default;
        if (GenerateStructureWithCache(circuit) is not { } structure)
            return false;

        if (structure.AdditionGuid == Guid.Empty)
            ExtraWorldsByGuid[structure.AdditionGuid = Guid.NewGuid()] = structure.AdditionWorld;

        guid = structure.AdditionGuid;
        return true;
    }

    public static bool TryGetUnpackingWorldGuid(IndexedPackedCircuitData circuit, out Guid guid)
    {
        guid = default;
        if (GenerateStructureWithCache(circuit) is not { } structure)
            return false;

        if (structure.UnpackingGuid == Guid.Empty)
            ExtraWorldsByGuid[structure.UnpackingGuid = Guid.NewGuid()] = structure.UnpackingWorld;

        guid = structure.UnpackingGuid;
        return true;
    }

    private static bool ActuallyConvert(ref PartialWorldData world, ComponentTypesManager target)
    {
        if (world.ComponentIDsMap.Values.Except(target.NumericIDsToTextIDs.Values).Any())
            return false;

        var oldComponentTypeToNew = world.ComponentIDsMap.ToDictionary(kvp => new ComponentType(kvp.Key), kvp => new ComponentType(target.TextIDsToNumericIDs[kvp.Value]));
        var components = world.OrderedComponentsAndAddresses.Select(kvp => (kvp.address, kvp.componentData.DuplicateAndChangeTypeTo(oldComponentTypeToNew[kvp.componentData.Type]))).ToList();
        var map = world.ComponentIDsMap;
        map = target.NumericIDsToTextIDs.Where(kvp => map.Values.Contains(kvp.Value)).ToDictionary();
        world = new PartialWorldData(map, components, world.AllWires, world.OnStates);
        return true;
    }
}
