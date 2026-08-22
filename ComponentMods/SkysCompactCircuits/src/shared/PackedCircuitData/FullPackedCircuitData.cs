using System;
using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Components;
using LogicWorld.SharedCode.Modding;
using LogicWorld.SharedCode.PartialWorlds;
using LogicWorld.SharedCode.Saving;
using UnityEngine;

namespace SkysCompactCircuits.Shared;

public class FullPackedCircuitData : IPackedCircuitData
{
    public Prefab ComponentPrefab { set; get; }
    public PartialWorldData PartialWorld { private set; get; }
    public ComponentAddress[] AddonAddresses { set; get; }
    public Vector3 TransformOffset { set; get; }
    public Vector3 TransformRotation { set; get; }
    public float TransformScale { set; get; } = 1;
    public Vector2Int Size { set; get; } = Vector2Int.one;
    public string Name { set; get; } = "";

    private byte[] EncodedWorldData;

    public byte[] Encode()
    {
        ByteWriter writer = new(EncodedWorldData.Length + 256);
        writer.WriteObject(IPackedCircuitData.Mode.Full)
            .Write(Name)
            .Write(EncodedWorldData)
            .Write(ComponentPrefab)
            .Write(TransformOffset)
            .Write(TransformRotation)
            .Write(TransformScale)
            .Write(Size);

        writer.Write(AddonAddresses?.Length ?? 0);
        foreach (var addon in AddonAddresses ?? [])
            writer.Write(addon);

        return writer.Finish();
    }

    public static FullPackedCircuitData Decode(ByteReader reader)
    {
        byte[] worldBytes;
        return new FullPackedCircuitData
        {
            Name = reader.ReadString(),
            EncodedWorldData = worldBytes = reader.ReadByteArray(),
            PartialWorld = PartialWorldUtilities.Deserialize(worldBytes),
            ComponentPrefab = reader.ReadPrefab(),
            TransformOffset = reader.ReadVector3(),
            TransformRotation = reader.ReadVector3(),
            TransformScale = reader.ReadFloat(),
            Size = reader.ReadVector2Int(),
            AddonAddresses = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadComponentAddress())],
        };
    }

    private FullPackedCircuitData() { }

    public FullPackedCircuitData(IPackedCircuitData copy)
    {
        while (copy is DeferredPackedCircuitData deferred)
            copy = deferred.Reference;
        ComponentPrefab = copy.ComponentPrefab;
        PartialWorld = copy.PartialWorld;
        AddonAddresses = copy.AddonAddresses;
        TransformOffset = copy.TransformOffset;
        TransformRotation = copy.TransformRotation;
        TransformScale = copy.TransformScale;
        Size = copy.Size;
        Name = copy.Name;
        if (copy is FullPackedCircuitData full)
            EncodedWorldData = full.EncodedWorldData;
        else
            EncodedWorldData = PartialWorldUtilities.Serialize(PartialWorld, ModRegistry.LoadedMods);
    }

    // This should only be used to create fully new circuits from scratch.
    public FullPackedCircuitData(PartialWorldData worldData) => EncodedWorldData = PartialWorldUtilities.Serialize(worldData, ModRegistry.LoadedMods);

    // This should only be used if you are fairly sure there will be no pre-existing matching circuits.
    public FullPackedCircuitData(IPackedCircuitData copy, PartialWorldData newWorldData) : this(copy)
    {
        PartialWorld = newWorldData;
        EncodedWorldData = PartialWorldUtilities.Serialize(PartialWorld, ModRegistry.LoadedMods);
    }

    // We cannot just change the partial world and re-encode it because the partial world serialization is non-deterministic.
    // (It changes based on installed mods, mod versions, and in theory dictionaries and hashsets have non-guaranteed iteration orders </3)
    public void UpdateInternalIndices(Dictionary<int, int> indexMap)
    {
        if (PartialWorld.ComponentIDsMap.FirstOrDefault(kvp => kvp.Value == "SkysCompactCircuits.PackedCircuit")
                is not { Key: { } localCircuitID, Value: not null })
            return; // No circuits here.

        var bytes = EncodedWorldData.ToArray(); // Copy the array.
        var pointer = SavingConstants.FileHeader.Length + 1 + 16 + 1 + 4 + 4; // Header.

        // We use reverse for loops so the values are only read once.
        for (var i = ReadInt(); i >= 1; i--) // Mod versions.
        {
            SkipArray(); // String data
            Skip(16);
        }

        for (var i = ReadInt(); i >= 1; i--)  // Component id map.
        {
            Skip(2);
            SkipArray(); // String data
        }

        for (var i = BitConverter.ToInt32(bytes, SavingConstants.FileHeader.Length + 1 + 16 + 1); i >= 1; i--)  // Component count.
            UpdateComponent();

        // The rest of the data we don't care about. It's unchanged and was already copied.
        EncodedWorldData = bytes;
        PartialWorld = PartialWorldUtilities.Deserialize(bytes);

        void UpdateComponent()
        {
            var type = BitConverter.ToUInt16(bytes, pointer + 8);
            Skip(38);
            SkipArray(4); // Input state ids.
            SkipArray(4); // Output state ids.

            var count = Math.Max(0, ReadInt()); // Custom data.
            // If anything's wrong, just skip the array as normal.
            if (type == localCircuitID && count >= 5
                && (IPackedCircuitData.Mode)bytes[pointer] is IPackedCircuitData.Mode.Indexed or IPackedCircuitData.Mode.Modified
                && indexMap.TryGetValue(BitConverter.ToInt32(bytes, pointer + 1), out var newIndex)
            )
                BitConverter.GetBytes(newIndex).CopyTo(bytes, pointer + 1);
            Skip(count);
        }

        int ReadInt() => BitConverter.ToInt32(bytes, (pointer += 4) - 4);
        void SkipArray(int size = 1) => Skip(Math.Max(0, ReadInt() * size)); // LW saves null as "size -1" but we don't want to advance -1 bytes.
        void Skip(int count) => pointer += count;
    }
}
