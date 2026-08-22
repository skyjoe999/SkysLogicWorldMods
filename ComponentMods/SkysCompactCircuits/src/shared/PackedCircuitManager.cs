using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LICC;
using LogicLog;
using LogicWorld.SharedCode.BinaryStuff;

namespace SkysCompactCircuits.Shared;

public class PackedCircuitManager
{
    private static readonly ILogicLogger Logger = LogicLogger.For<PackedCircuitManager>();
    public static readonly Dictionary<int, IPackedCircuitData> CircuitDataByIndex = [];
    public static readonly Dictionary<int, List<int>> IndicesByHash = [];
    public static int HighestIndexSoFar = 0;

    public static event Action<int, byte[]> OnIndexAdded;

    public static IPackedCircuitData LookupIndexed(int index) => CircuitDataByIndex.GetValueOrDefault(index);

    public static IPackedCircuitData Decode(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0)
            throw new("Cannot decode data with zero length");

        using MemoryByteReader reader = new(bytes);
        var mode = (IPackedCircuitData.Mode)reader.Read(typeof(IPackedCircuitData.Mode));
        return mode switch
        {
            IPackedCircuitData.Mode.Error => throw new("Cannot decode invalid circuit data"),
            IPackedCircuitData.Mode.Full => FullPackedCircuitData.Decode(reader),
            IPackedCircuitData.Mode.Indexed => IndexedPackedCircuitData.Decode(reader),
            IPackedCircuitData.Mode.SubassemblyTracker => SubassemblyTrackerPackedCircuitData.Decode(reader),
            IPackedCircuitData.Mode.SubassemblyItem => SubassemblyItemPackedCircuitData.Decode(reader),
            IPackedCircuitData.Mode.Compressed => CompressedPackedCircuitData.Decode(reader),
            IPackedCircuitData.Mode.Modified => ModifiedIndexedPackedCircuitData.Decode(reader),
            _ => throw new($"Unexpected formatting mode: {mode} (Maybe try updating the mod?)")
        };
    }

    public static IndexedPackedCircuitData DecodeAndIndex(IPackedCircuitData data) => data as IndexedPackedCircuitData ?? DecodeAndIndex(data.Encode(), data);
    public static IndexedPackedCircuitData DecodeAndIndex(byte[] bytes) => DecodeAndIndex(Decode(bytes)); // We can't use the bytes because of subassembly trackers </3
    private static IndexedPackedCircuitData DecodeAndIndex(byte[] bytes, IPackedCircuitData data)
    {
        if (data is null)
            return null;
        // if this data is already indexed, no need to index it! ^^
        if (data is IndexedPackedCircuitData indexed)
            return indexed;

        // the data needs to be added to the index
        // but equality checking involves re-encoding all the data so we start with hashing
        var hash = ComputeHash(bytes);
        if (IndicesByHash.TryGetValue(hash, out var indices))
        {
            // now we need to enure they are *actually* equal
            foreach (var index in indices)
                if (CircuitDataByIndex[index].Encode().SequenceEqual(bytes))
                    return new(index);
            Logger.Trace($"Hash collision with hash {hash}");
        }

        // match not found, time to add it
        Logger.Trace($"Allocating new index {HighestIndexSoFar + 1}");
        RegisterIndex(++HighestIndexSoFar, bytes, data, hash);
        OnIndexAdded?.Invoke(HighestIndexSoFar, bytes);

        return new(HighestIndexSoFar);
    }

    public static void RegisterIndex(int index, byte[] bytes) => RegisterIndex(index, Decode(bytes));
    public static void RegisterIndex(int index, IPackedCircuitData data) => RegisterIndex(index, data.Encode(), data);
    private static void RegisterIndex(int index, byte[] bytes, IPackedCircuitData data, int? hash = null)
    {
        hash ??= ComputeHash(bytes);
        if (!IndicesByHash.TryGetValue(hash.Value, out var indices))
            IndicesByHash.Add(hash.Value, indices = new(1));

        // match not found, time to add it
        indices.Add(index);
        CircuitDataByIndex.Add(index, data);
    }

    public static int ComputeHash(params byte[] data)
    {
        const int p = 16777619;
        int hash = -2128831035;
        for (int i = 0; i < data.Length; i++)
            hash = (hash ^ data[i]) * p;
        return hash;
    }

    public static bool TryGetIndex(byte[] bytes, out int index)
    {
        index = -1;
        if (bytes is null || bytes.Length == 0 ||
            (IPackedCircuitData.Mode)bytes[0] is not (IPackedCircuitData.Mode.Indexed or IPackedCircuitData.Mode.Modified)
        )
            return false;

        index = BitConverter.ToInt32(bytes, 1);
        return true;
    }

    public static bool IsIndexValid(int index) => CircuitDataByIndex.ContainsKey(index);

    public static void DeserializeData(byte[] data)
    {
        using var reader = new MemoryByteReader(data);
        DeserializeData(reader);
    }

    public static void DeserializeData(ByteReader reader)
    {
        ClearAllData();

        var count = reader.ReadInt32();
        Logger.Trace($"Loading {count} packed circuits");
        for (var i = 0; i < count; i++)
        {
            var index = reader.ReadInt32();
            var raw = reader.ReadByteArray();
            var hash = ComputeHash(raw);

            if (!IndicesByHash.TryGetValue(hash, out var indices))
                IndicesByHash.Add(hash, indices = new(1));
            indices.Add(index);

            CircuitDataByIndex[index] = Decode(raw);
        }
        HighestIndexSoFar = CircuitDataByIndex.Count == 0 ? 0 : CircuitDataByIndex.Keys.Max();
    }
    public static byte[] SerializeData()
    {
        var writer = new ByteWriter();
        writer.Write(CircuitDataByIndex.Count);
        foreach (var (index, data) in CircuitDataByIndex)
            writer.Write(index).Write(data.Encode());

        return writer.Finish();
    }

    [Command("CompactCircuits.List")]
    public static void ListCommand()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Current circuits:");
        if (CircuitDataByIndex.Count == 0)
            builder.AppendLine($"  None");
        foreach (var (index, data) in CircuitDataByIndex)
            builder.AppendLine($"  {index}: {Convert(data)}");
        Logger.Info(builder.ToString().TrimEnd());
        static string Convert(IPackedCircuitData data) => data switch
        {
            ModifiedIndexedPackedCircuitData modified => $"Modified {modified.Index} ({modified.AddonMap.Length} addons, {modified.ToggledOutStates.Length} states, {modified.ChangedCustomDatas} datas)",
            IndexedPackedCircuitData indexed => $"Indexed {indexed.Index} ({Convert(indexed.Reference)})",
            FullPackedCircuitData full => $"Full {full.Encode().Length}",
            SubassemblyTrackerPackedCircuitData tracker => $"Tracker [{string.Join(", ", tracker.IndexMap.Select(pair => $"{pair.Key}: ({Convert(pair.Value)})"))}]",
            SubassemblyItemPackedCircuitData item => $"SubItem {item.Index} ({Convert(item.Reference)})",
            CompressedPackedCircuitData compressed => $"Compressed {compressed.Encode().Length} ({Convert(compressed.Reference)})",
            null => "Null",
            _ => $"Unknown?",
        };
    }

    public static void ClearAllData()
    {
        CircuitDataByIndex.Clear();
        IndicesByHash.Clear();
        HighestIndexSoFar = 0;
    }
}
