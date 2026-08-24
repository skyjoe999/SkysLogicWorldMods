using System;
using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities.Collections;
using LogicAPI.Data;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysCompactCircuits.Server;

public static class ModifiedIndexResolver
{
    public static PartialWorldData ExpandModified(IndexedPackedCircuitData circuit)
    {
        var world = circuit.PartialWorld;
        if (!world.ComponentIDsMap.Values.Contains("SkysCompactCircuits.PackedCircuit"))
            return world;

        var idMap = new TwoWayDictionary<ushort, string>(world.ComponentIDsMap.ToDictionary());
        var localCircuitID = idMap["SkysCompactCircuits.PackedCircuit"];

        if (!circuit.PartialWorld.OrderedComponentsAndAddresses.Any(
            pair => pair.componentData.Type.NumericID == localCircuitID &&
            pair.componentData.CustomData.Length != 0 &&
            pair.componentData.CustomData[0] == (byte)IPackedCircuitData.Mode.Modified
        ))
            return world;

        var next = (
            stateID: world.OnStates
                .Concat(world.OrderedComponentsAndAddresses.SelectMany(component => component.componentData.OutputInfos.Cast<IPegInfo>().Concat(component.componentData.InputInfos.Cast<IPegInfo>())).Select(peg => peg.StateID))
                .Concat(world.AllWires.Select(wire => wire.StateID))
                .Append(-1).Max() + 1,
            componentAddress: world.OrderedComponentsAndAddresses.Select(component => component.address.ID)
                .Concat(circuit.AddonAddresses.Select(address => address.ID)).Max() + 1
        );

        var components = new List<(ComponentAddress address, ComponentData componentData)>(world.OrderedComponentsAndAddresses.Count * 2);
        var wires = world.AllWires.ToList();
        var onStates = world.OnStates.ToHashSet();

        var parentQueue = new Queue<ComponentAddress>();
        var additionalQueue = new Queue<(ComponentAddress address, IEnumerator<List<(ComponentAddress address, ComponentData componentData)>> extraChildren)>();
        parentQueue.Enqueue(default);
        var prev = ComponentAddress.Empty;

        foreach (var (address, data) in world.OrderedComponentsAndAddresses)
        {
            while (parentQueue.Peek() != data.Parent)
                if (parentQueue.Dequeue() == (additionalQueue.TryPeek(out var value) ? value.address : null))
                    AddLayer(additionalQueue.Dequeue().extraChildren);

            if (data.Type.NumericID == localCircuitID && ExpandInnerCircuit(address, data, ref idMap, ref next) is { } innerWorld)
            {
                AddLayer(GetLayers(innerWorld).GetEnumerator());

                wires.AddRange(innerWorld.AllWires);
                onStates.UnionWith(innerWorld.OnStates);
                parentQueue.Enqueue(prev = address);
            }
            else
            {
                components.Add((address, data));
                parentQueue.Enqueue(prev = address);
            }
        }


        // Flush the remaining inner circuits.
        while (additionalQueue.TryDequeue(out var child))
            AddLayer(child.extraChildren);

        return new(idMap.Forwards, components, wires, onStates);

        void AddLayer(IEnumerator<List<(ComponentAddress address, ComponentData componentData)>> extraChildren)
        {
            if (extraChildren.MoveNext())
            {
                components.AddRange(extraChildren.Current);
                additionalQueue.Enqueue((prev, extraChildren));
            }
        }
    }

    public static PartialWorldData ExpandInnerCircuit(ComponentAddress address, ComponentData data, ref TwoWayDictionary<ushort, string> idMap, ref (int stateID, uint componentAddress) next)
    {
        // Start but reading in the data.
        if (PackedCircuitManager.Decode(data.CustomData) is not ModifiedIndexedPackedCircuitData modifier)
            return null;

        // Get the world to insert.
        if (PackedCircuitStructureManager.GenerateStructureWithCache(modifier) is not { AdditionWorld: { } addition, ExportAddresses: { } exportAddresses })
            return null;

        var (nextStateID, nextComponentAddress) = next;

        var addressMap = modifier.AddonMap.ToDictionary(); // Addons need to keep their original mapped addresses.
        addressMap[addition.OrderedComponentsAndAddresses[0].address] = address; // Map the root to the placed component.
        addressMap[addition.OrderedComponentsAndAddresses[0].componentData.Parent] = data.Parent; // Map the old parent (probably empty) to the new parent.

        // Map the export pegs to their counterpart.
        var exportAddressesSet = exportAddresses.Select(address => address.ComponentAddress).ToHashSet();
        var exportDatas = addition.OrderedComponentsAndAddresses.Where(pair => exportAddressesSet.Contains(pair.address)).ToDictionary();
        var statesMap = exportAddresses.Where(address => address.IsInputAddress()) // We dont care about export buffers, those will be handled by the components themselves.
            .Select((address, i) => (index: i, exportData: exportDatas.GetValueOrDefault(address.ComponentAddress), pegIndex: address.PegIndex))
            .Where(info => info.exportData is not null)
            .ToDictionary(info => info.exportData.InputInfos[info.pegIndex].StateID, info => data.InputInfos[info.index].StateID);

        // Convert the component types.
        var nextType = (ushort)(idMap.Forwards.Keys.Max() + 1);
        var oldComponentTypeToNew = new Dictionary<ComponentType, ComponentType>();

        foreach (var (key, value) in addition.ComponentIDsMap)
        {
            var newKey = key;
            if (idMap.Backwards.TryGetValue(value, out var existing))
                newKey = existing;
            else
                idMap[idMap.ContainsKey(key) ? newKey = nextType++ : key] = value;
            oldComponentTypeToNew[new(key)] = new(newKey);
        }

        var onStates = addition.OnStates.ToHashSet();

        // Setup the conversions from the modifier
        var changedCustomDatas = modifier.ChangedCustomDatas.ToDictionary();

        foreach (var stateID in modifier.ToggledOutStates)
            if (!onStates.Remove(stateID))
                onStates.Add(stateID);

        // We need to adjust the state ids for the root component
        var root = ConvertComponent(addition.OrderedComponentsAndAddresses[0]);
        root.data = root.data.Duplicate();
        (root.data as IEditableComponentData).InputInfos = [.. data.InputInfos];
        (root.data as IEditableComponentData).LocalPositionFixed = data.LocalPositionFixed;
        (root.data as IEditableComponentData).LocalRotation = data.LocalRotation;

        // Convert the rest by bruit force.
        var result = new PartialWorldData(
            idMap.Forwards,
            [root, .. addition.OrderedComponentsAndAddresses.Skip(1).Select(ConvertComponent)],
            [.. addition.AllWires.Select(wire => new Wire(ConvertPegAddress(wire.Point1), ConvertPegAddress(wire.Point2), ConvertID(wire.StateID), wire.Rotation))],
            [.. onStates.Select(ConvertID)]
        );


        next = (nextStateID, nextComponentAddress);
        return result;

        // Convert the addresses and peg states.
        (ComponentAddress address, ComponentData data) ConvertComponent((ComponentAddress address, ComponentData data) component)
        {
            var data = (IEditableComponentData)component.data.DuplicateAndChangeTypeTo(oldComponentTypeToNew[component.data.Type]);

            // Map the circuit state ids.
            data.InputInfos = [.. data.InputInfos.Select(info => new InputInfo(ConvertID(info.StateID)))];
            data.OutputInfos = [.. data.OutputInfos.Select(info => new OutputInfo(ConvertID(info.StateID)))];

            // Modify the custom data when applicable.
            if (changedCustomDatas.TryGetValue(component.address, out var newData))
                data.CustomData = newData;

            // Update the addresses.
            data.Parent = ConvertAddress(data.Parent);
            return (ConvertAddress(component.address), (ComponentData)data);
        }

        int ConvertID(int id) => statesMap.TryGetValue(id, out var value) ? value : statesMap[id] = nextStateID++;
        ComponentAddress ConvertAddress(ComponentAddress address) => addressMap.TryGetValue(address, out var value) ? value : addressMap[address] = new(nextComponentAddress++);
        PegAddress ConvertPegAddress(PegAddress address) => new(ConvertAddress(address.ComponentAddress), address.PegIndex, address.PegType);
    }

    public static void ConsolidateModified(ref IPackedCircuitData circuit)
    {
        var world = circuit.PartialWorld;
        if (!world.ComponentIDsMap.Values.Contains("SkysCompactCircuits.PackedCircuit"))
            return;
        var localCircuitID = world.ComponentIDsMap.First(kvp => kvp.Value == "SkysCompactCircuits.PackedCircuit").Key;

        var parentQueue = new Queue<(ComponentAddress address, List<(ComponentAddress address, ComponentData componentData)> inner)>();
        parentQueue.Enqueue(default);

        var components = new List<(ComponentAddress address, ComponentData componentData)>();
        var innerCircuits = new List<(int componentIndex, IndexedPackedCircuitData indexed, List<(ComponentAddress address, ComponentData componentData)> children)>();

        foreach (var (address, data) in world.OrderedComponentsAndAddresses)
        {
            while (parentQueue.Peek().address != data.Parent)
                parentQueue.Dequeue();

            if (parentQueue.Peek().inner is { } inner)
            {
                inner.Add((address, data));
                parentQueue.Enqueue((address, inner));
            }
            else if (data.Type.NumericID == localCircuitID)
            {
                if (PackedCircuitManager.Decode(data.CustomData) is not IndexedPackedCircuitData indexed || PackedCircuitStructureManager.GenerateStructureWithCache(indexed) is not { } structure)
                    continue;

                var innerChildren = new List<(ComponentAddress address, ComponentData componentData)>(structure.AdditionWorld.ComponentsCount);
                innerCircuits.Add((components.Count, indexed, innerChildren));

                components.Add((address, data));
                parentQueue.Enqueue((address, innerChildren));
            }
            else
            {
                components.Add((address, data));
                parentQueue.Enqueue((address, null));
            }
        }

        foreach (var (componentIndex, indexed, children) in innerCircuits)
        {
            var modifier = ConsolidateInnerCircuit(circuit, components[componentIndex].address, indexed, children);
            var newData = components[componentIndex].componentData.Duplicate();
            ((IEditableComponentData)newData).CustomData = modifier.Encode();
            components[componentIndex] = (components[componentIndex].address, newData);
        }

        if (innerCircuits.Count > 0)
        {
            var removedComponents = innerCircuits.SelectMany(pair => pair.children.Select(child => child.address)).ToHashSet();
            var usedTypes = components.Select(pair => pair.componentData.Type.NumericID).ToHashSet();
            var wires = world.AllWires.Where(wire => !removedComponents.Contains(wire.Point1.ComponentAddress) && !removedComponents.Contains(wire.Point2.ComponentAddress));
            var componentIDsMap = world.ComponentIDsMap.Where(kvp => usedTypes.Contains(kvp.Key)).ToDictionary();
            circuit = new FullPackedCircuitData(circuit, new PartialWorldData(componentIDsMap, components, [.. wires], world.OnStates));
            circuit = new CompressedPackedCircuitData((FullPackedCircuitData)circuit);
        }
    }

    private static ModifiedIndexedPackedCircuitData ConsolidateInnerCircuit(IPackedCircuitData circuit, ComponentAddress root, IndexedPackedCircuitData indexed, List<(ComponentAddress address, ComponentData componentData)> children)
    {
        var structure = PackedCircuitStructureManager.GenerateStructureWithCache(indexed).Value;
        var childMap = GetChildMap(root, children, structure) ?? throw new($"Could not find matching circuit for {root} (circuit index {indexed.Index})");

        var addonAddresses = circuit.AddonAddresses.ToHashSet();
        var childLookup = children.ToDictionary();

        var addonMap = new List<(ComponentAddress referenced, ComponentAddress child)>();
        var changedCustomDatas = new List<(ComponentAddress referenced, byte[] data)>();

        // We have to figure out how the states are mapped so we can do the toggles. (We only care about outputs.)
        var statesMap = new Dictionary<int, int>();

        foreach (var (referenced, referencedData) in structure.AdditionWorld.OrderedComponentsAndAddresses)
        {
            if (!childMap.TryGetValue(referenced, out var child) || !childLookup.TryGetValue(child, out var childData))
                continue; // This is probably not good </3

            if (addonAddresses.Remove(referenced)) // We use remove here to avoid duplicates.
                addonMap.Add((referenced, child));

            foreach (var (rOut, cOut) in referencedData.OutputInfos.Zip(childData.OutputInfos))
                statesMap[cOut.StateID] = rOut.StateID;

            if ((childData.CustomData is null) != (referencedData.CustomData is null) || !(referencedData.CustomData?.SequenceEqual(childData.CustomData) ?? true))
                changedCustomDatas.Add((referenced, childData.CustomData));
        }

        var toggles = statesMap.Where(kvp => indexed.PartialWorld.OnStates.Contains(kvp.Value) != circuit.PartialWorld.OnStates.Contains(kvp.Key)).Select(kvp => kvp.Value);

        return new ModifiedIndexedPackedCircuitData(indexed.Index)
        {
            ToggledOutStates = [.. toggles.Order()],
            AddonMap = [.. addonMap],
            ChangedCustomDatas = [.. changedCustomDatas],
        };

        static Dictionary<ComponentAddress, ComponentAddress> GetChildMap(ComponentAddress root, List<(ComponentAddress address, ComponentData componentData)> children, PackedCircuitStructure structure)
        {
            return structure.AdditionWorld.OrderedComponentsAndAddresses.Select(pair => pair.componentData.Type).SequenceEqual(children.Select(pair => pair.componentData.Type))
                ? structure.OriginalChildAddresses.Zip(children.Select(child => child.address)).ToDictionary()
                // This adds an assumption (the component existing) but I cannot be bothered to reimplement that awful function right now.
                : (root.GetLogicComponent() is PackedCircuit component) ? component.ChildMap ??= PackedCircuit.GenerateChildMap(component, structure) : null;
        }
    }

    /// <summary> Separates a single root partial worlds components by their number of ancestors. Starting with the root, then its children then grandchildren etc. </summary>
    private static IEnumerable<List<(ComponentAddress address, ComponentData componentData)>> GetLayers(PartialWorldData world)
    {
        var parentQueue = new Queue<(ComponentAddress address, ComponentData data)>();
        var lastInLayer = world.OrderedComponentsAndAddresses[0].componentData.Parent; // Not empty for our use case.
        var prev = lastInLayer;

        parentQueue.Enqueue((prev, null));
        foreach (var (address, data) in world.OrderedComponentsAndAddresses)
        {
            while (parentQueue.Peek().address != data.Parent)
                if (parentQueue.Dequeue().address == lastInLayer)
                {
                    lastInLayer = prev;
                    yield return parentQueue.ToList();
                }
            parentQueue.Enqueue((prev = address, data));
        }

        // Get rid of the last layer and flush the queue.
        while(parentQueue.Dequeue().address != lastInLayer);
        yield return parentQueue.ToList();
    }
}
