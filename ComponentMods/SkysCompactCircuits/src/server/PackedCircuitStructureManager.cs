using System;
using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicWorld.SharedCode.PartialWorlds;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Server;
using SkysGeneralLib.Shared.TypeExtensions;
using UnityEngine;

namespace SkysCompactCircuits.Server;

public static class PackedCircuitStructureManager
{
    public static readonly HashSet<string> TextIDsToExclude = [];
    public static void RegisterExcludedType(string textID) => TextIDsToExclude.Add(textID);

    // This one is session only, saving this data to disk is more trouble than it's worth
    // (Might need to change to reduce startup lag...)
    public static readonly Dictionary<int, PackedCircuitStructure?> StructuresByIndex = [];
    public static readonly Dictionary<Guid, PartialWorldData> AdditionWorldsByGuid = [];

    public static PackedCircuitStructure? GenerateStructureWithCache(IndexedPackedCircuitData circuit)
    {
        return StructuresByIndex.TryGetValue(circuit.Index, out var structure) ? structure
            : (StructuresByIndex[circuit.Index] = GenerateStructure(circuit.PartialWorld, circuit.Encode(), [.. circuit.AddonAddresses ?? []]));
    }

    public static PackedCircuitStructure? GenerateStructure(PartialWorldData world, byte[] rootData, HashSet<ComponentAddress> forceInclude = null)
    {
        if (!world.IsCompatibleWith(Services.ComponentTypesManager))
            return null;

        var includedComponents = FilterForRelevantComponents(world.OrderedComponentsAndAddresses, world.ComponentIDsMap, forceInclude ?? [], out var excludedTypes).ToList();
        var componentAddressToIndices = includedComponents.Select((v, i) => (v.address, i)).ToDictionary(p => p.address, p => (ushort)p.i);

        var wires = GenerateWires(includedComponents, world.AllWires, world.OrderedComponentsAndAddresses).ToList();

        var onStates = includedComponents.SelectMany(p => p.componentData.OutputInfos).Select(o => o.StateID).ToHashSet();
        onStates.IntersectWith(world.OnStates);

        var localCircuitID = world.ComponentIDsMap.Where(p => p.Value == "SkysCompactCircuits.PackedCircuit").Aggregate((ushort?)null, (_, v) => v.Key);
        var componentIDsMap = world.ComponentIDsMap.ToDictionary(p => p.Key, p => p.Value); // clone the dictionary

        var exportIndices = world.ComponentIDsMap.Where(p => p.Value == "SkysCompactCircuits.ExportPeg").Aggregate((ushort?)null, (_, v) => v.Key) is ushort exportPegID
                ? FindRootExportPegs(includedComponents.Skip(1), exportPegID, localCircuitID).Select(a => componentAddressToIndices[a]).ToArray() : [];

        var root = (IEditableComponentData)new ComponentData(new(localCircuitID ?? (ushort)Enumerable.Range(1, componentIDsMap.Count + 1).First(i => !componentIDsMap.ContainsKey((ushort)i))));
        root.InputInfos = new InputInfo[exportIndices.Length];
        root.OutputInfos = [];
        root.LocalRotation = Quaternion.identity;
        root.CustomData = rootData;
        includedComponents[0] = (includedComponents[0].address, (ComponentData)root);

        if (!localCircuitID.HasValue)
            componentIDsMap[((ComponentData)root).Type.NumericID] = "SkysCompactCircuits.PackedCircuit";

        return new()
        {
            AdditionWorld = PartialWorldUtilities.ConvertComponentTypes(new(
                componentIDsMap,
                includedComponents,
                wires,
                onStates
            ), Services.ComponentTypesManager),
            ExportIndices = exportIndices,
            OriginalChildAddresses = [.. includedComponents.Select(p => p.address)],
        };
    }

    public static IEnumerable<(ComponentAddress address, ComponentData componentData)> FilterForRelevantComponents(IReadOnlyList<(ComponentAddress address, ComponentData componentData)> orderedComponentsAndAddresses, IReadOnlyDictionary<ushort, string> componentIDsMap, HashSet<ComponentAddress> alwaysInclude, out HashSet<ushort> excludedTypes)
    {

        var allTypes = componentIDsMap?.ToDictionary(p => p.Value, p => p.Key);
        excludedTypes = [.. TextIDsToExclude.Intersect(allTypes.Keys).Select(id => allTypes[id])];

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

    public static IEnumerable<ComponentAddress> FindRootExportPegs(IEnumerable<(ComponentAddress address, ComponentData componentData)> orderedComponentsAndAddresses, ushort exportID, ushort? circuitID = null)
    {
        var innerAddresses = new HashSet<ComponentAddress>();
        if (circuitID is null)
        {
            foreach (var (address, _) in orderedComponentsAndAddresses.Where(c => c.componentData.Type.NumericID == exportID))
                yield return address;
            yield break;
        }

        foreach (var (address, componentData) in orderedComponentsAndAddresses)
            if (componentData.Type.NumericID == circuitID || innerAddresses.Contains(componentData.Parent))
                innerAddresses.Add(address);
            else if (componentData.Type.NumericID == exportID)
                yield return address;
    }

    public static IEnumerable<Wire> GenerateWires(List<(ComponentAddress address, ComponentData componentData)> includedComponents, IReadOnlyList<Wire> wires, IReadOnlyList<(ComponentAddress address, ComponentData componentData)> allComponents)
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

    public static bool TryGetAdditionWorld(Guid guid, out PartialWorldData additionWorld) => AdditionWorldsByGuid.TryGetValue(guid, out additionWorld);
    public static bool TryGetAdditionWorldGuid(IndexedPackedCircuitData circuit, out Guid guid)
    {
        guid = default;
        if (GenerateStructureWithCache(circuit) is not { } structure)
            return false;

        if (structure.AdditionGuid == Guid.Empty)
            AdditionWorldsByGuid[structure.AdditionGuid = Guid.NewGuid()] = structure.AdditionWorld;

        guid = structure.AdditionGuid;
        return true;
    }
}
