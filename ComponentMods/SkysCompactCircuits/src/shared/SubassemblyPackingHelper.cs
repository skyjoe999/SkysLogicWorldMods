using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;

namespace SkysCompactCircuits.Shared;

public static class SubassemblyPackingHelper
{
    /// <summary> Finds all the packed circuits in the world that are indexed circuits and their index in the component list. (Does not try to load them.) </summary>
    public static IEnumerable<(int childIndex, int circuitIndex)> GetIndexedCircuitsFromPartialWorld(PartialWorldData world)
    {
        if (world.ComponentIDsMap.FirstOrDefault(kvp => kvp.Value == "SkysCompactCircuits.PackedCircuit")
                is not { Key: { } localCircuitID, Value: not null })
            yield break; // No circuits here.

        for (int i = 0; i < world.ComponentsCount; i++)
        {
            var data = world.OrderedComponentsAndAddresses[i].componentData;
            if (data.Type.NumericID == localCircuitID && PackedCircuitManager.TryGetIndex(data.CustomData, out var circuitIndex))
                yield return (i, circuitIndex);
        }
    }

    /// <summary> Creates a partial world that matches the original except with the provided custom data replacing the original at the indices provided. </summary>
    public static PartialWorldData ReplaceCustomDatas(PartialWorldData world, IEnumerable<(int childIndex, byte[] newData)> circuits)
    {
        var components = world.OrderedComponentsAndAddresses.ToList();
        foreach (var ((childIndex, newData), index) in circuits.Select((v, i) => (v, i)))
        {
            var (address, componentData) = components[childIndex];
            components[childIndex] = (address, componentData.Duplicate());
            ((IEditableComponentData)components[childIndex].componentData).CustomData = newData;
        }

        return new(world.ComponentIDsMap, components, world.AllWires, world.OnStates);
    }

    /// <summary> Converts a mapping of indices to circuits by assigning new indices to them and converting any references in their partial worlds. </summary>
    public static Dictionary<int, IPackedCircuitData> UpdateIndexedCircuitsInInternalWorldsAndIndex(Dictionary<int, IPackedCircuitData> rawMapping)
    {
        var newMapping = new Dictionary<int, IPackedCircuitData>();
        var indexMap = new Dictionary<int, int>();
        var circuitsPerWorld = rawMapping.ToDictionary(kvp => kvp.Key, kvp => GetIndexedCircuitsFromPartialWorld(kvp.Value.PartialWorld).ToArray());
        var dependencies = circuitsPerWorld.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(pair => pair.circuitIndex).ToHashSet());

        while (dependencies.Count != 0)
        {
            var (index, subCircuits) = dependencies.FirstOrDefault(kvp => kvp.Value.All(newMapping.ContainsKey));
            if (subCircuits is null) // None found;
                throw new("Found circular dependency");

            var circuit = rawMapping[index];
            if (subCircuits.Count != 0)
            {
                if (((circuit as CompressedPackedCircuitData)?.Reference ?? circuit) is not FullPackedCircuitData full)
                    throw new($"Found non-full data ({circuit.GetType()?.ToString() ?? "null"})");

                full.UpdateInternalIndices(indexMap);
            }

            var indexed = PackedCircuitManager.DecodeAndIndex(circuit);
            newMapping[index] = indexed;
            if (index != indexed.Index)
                indexMap[index] = indexed.Index;
            dependencies.Remove(index);
        }

        return newMapping;
    }
}
