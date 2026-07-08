using System;
using System.Collections.Generic;
using System.Linq;
using LogicLog;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.ExtraData;

namespace SkysCompactCircuits.Shared;

public class PackedCircuitManager
{
    private static readonly ILogicLogger Logger = LogicLogger.For<PackedCircuitManager>();
    public static readonly Dictionary<int, IPackedCircuitData> CircuitDataByIndex = [];
    public static readonly Dictionary<int, List<int>> IndicesByHash = [];
    public static int HighestIndexSoFar = 0;

    public static IPackedCircuitData LookupIndexed(int index) => CircuitDataByIndex[index];

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
            _ => throw new($"Unexpected formatting mode: {mode} (Maybe try updating the mod?)")
        };
    }

    public static IndexedPackedCircuitData DecodeAndIndex(IPackedCircuitData data) => DecodeAndIndex(data.Encode(), data);
    public static IndexedPackedCircuitData DecodeAndIndex(byte[] bytes) => DecodeAndIndex(bytes, Decode(bytes));
    private static IndexedPackedCircuitData DecodeAndIndex(byte[] bytes, IPackedCircuitData data)
    {
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
        else
            IndicesByHash.Add(hash, indices = new(1));

        // match not found, time to add it
        indices.Add(++HighestIndexSoFar);
        Logger.Trace($"Allocating new index {HighestIndexSoFar}");
        CircuitDataByIndex.Add(HighestIndexSoFar, data);
        ExtraDataManager.WriteToExtraData();

        return new(HighestIndexSoFar);
    }

    public static int ComputeHash(params byte[] data)
    {
        const int p = 16777619;
        int hash = -2128831035;
        for (int i = 0; i < data.Length; i++)
            hash = (hash ^ data[i]) * p;
        return hash;
    }

    public static readonly Dictionary<int, List<Action>> WaitingOnIndex = [];
    public static IPackedCircuitData TryDecode(byte[] bytes, Action runWhenIndexAvailable = null)
    {
        using MemoryByteReader reader = new(bytes);
        if ((IPackedCircuitData.Mode)reader.Read(typeof(IPackedCircuitData.Mode)) != IPackedCircuitData.Mode.Indexed)
            return Decode(bytes);
        try { return Decode(bytes); }
        catch (KeyNotFoundException)
        {
            if (runWhenIndexAvailable is null)
                return null;
            var index = reader.ReadInt32();
            Logger.Trace($"Adding wait for {index}");
            if (!WaitingOnIndex.TryGetValue(index, out var list))
                WaitingOnIndex[index] = list = new(1);
            list.Add(runWhenIndexAvailable);
            return null;
        }
    }

    public static void DeserializeData(byte[] data)
    {
        HighestIndexSoFar = 0;
        CircuitDataByIndex.Clear();
        IndicesByHash.Clear();

        using var reader = new MemoryByteReader(data);
        var count = reader.ReadInt32();
        Logger.Trace($"Loading {count} packed circuits");
        for (var i = 0; i < count; i++)
        {
            var index = reader.ReadInt32();
            var raw = reader.ReadByteArray();
            var hash = ComputeHash(raw);

            if (IndicesByHash.TryGetValue(hash, out var indices))
                indices.Add(index);
            else
                IndicesByHash.Add(hash, [index]);
            CircuitDataByIndex[index] = Decode(raw);

            if (index > HighestIndexSoFar)
                HighestIndexSoFar = index;
            if (WaitingOnIndex.TryGetValue(index, out List<Action> list))
            {
                Logger.Trace($"{index} was being waiting on {list.Count} components");
                foreach (var action in list)
                    action();
                WaitingOnIndex.Remove(index);
            }
        }
    }
    public static byte[] SerializeData()
    {
        var writer = new ByteWriter();
        writer.Write(CircuitDataByIndex.Count);
        foreach (var (index, data) in CircuitDataByIndex)
            writer.Write(index).Write(data.Encode());

        return writer.Finish();
    }

    public static class ExtraDataManager
    {
        public static ExtraDataAccessor<string> ExtraData;

        public static bool IgnoreNextRead;
        public static void SetupExtraData(ExtraData extraData)
        {
            ExtraData = extraData.GetDataAccessor("SkysCompactCircuits.PackedCircuitDatas", "");
            Logger.Trace("Setting up extra data");

            ExtraData.RunOnEveryDataUpdate(data =>
            {
                if (IgnoreNextRead)
                {
                    Logger.Trace("Ignoring extra data write");
                    IgnoreNextRead = false;
                    return;
                }

                Logger.Trace("Reloading extra data");
                ReadFromExtraData(data);
            });
        }

        private static void ReadFromExtraData(string data) => DeserializeData(Convert.FromBase64String(data));
        public static void ReadFromExtraData() => ReadFromExtraData(ExtraData.Data);

        // This should only ever *add* data except on server world load
        // If it were to remove data then the runtime lookups would be wrong
        // (And also maybe a bug is about to wipe everything and we don't want that even if backups do exist)
        public static void WriteToExtraData(bool checkSize = true)
        {
            Logger.Trace("Overwriting extra data");
            var newData = Convert.ToBase64String(SerializeData());
            if (checkSize && newData.Length < ExtraData.Data.Length)
                throw new($"New data ({newData.Length}*6b) shorter than existing data ({ExtraData.Data.Length}*6b). This data should only ever be added to!");
            IgnoreNextRead = true;
            ExtraData?.SetData(newData);
        }
    }
}
