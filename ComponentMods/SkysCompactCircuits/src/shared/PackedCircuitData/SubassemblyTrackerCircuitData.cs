using System;
using System.Collections.Generic;
using System.Linq;
using LogicWorld.SharedCode.BinaryStuff;

namespace SkysCompactCircuits.Shared;

public class SubassemblyTrackerPackedCircuitData(Dictionary<int, IPackedCircuitData> circuits, int referencedIndex) : DeferredPackedCircuitData(circuits[referencedIndex])
{
    public static SubassemblyTrackerPackedCircuitData MostRecentDecoded;

    public Dictionary<int, IPackedCircuitData> IndexMap = circuits;
    public int ReferencedIndex = referencedIndex;

    public override byte[] Encode()
    {
        var writer = new ByteWriter();
        writer.WriteObject(IPackedCircuitData.Mode.SubassemblyTracker);

        writer.Write(ReferencedIndex);

        writer.Write(IndexMap.Count);
        foreach (var (index, circuit) in IndexMap)
            writer.Write(index).Write(new CompressedPackedCircuitData(circuit).Encode());

        return writer.Finish();
    }

    public static IPackedCircuitData Decode(ByteReader reader)
    {
        var referencedIndex = reader.ReadInt32();
        var mapping = Enumerable
            .Range(0, reader.ReadInt32())
            .Select(_ => (index: reader.ReadInt32(), data: PackedCircuitManager.Decode(reader.ReadByteArray())))
            .ToDictionary(pair => pair.index, pair => pair.data);

#if !LW_SIDE_CLIENT
        mapping = SubassemblyPackingHelper.UpdateIndexedCircuitsInInternalWorldsAndIndex(mapping);
#endif

        MostRecentDecoded = new SubassemblyTrackerPackedCircuitData(mapping, referencedIndex);
        return MostRecentDecoded.Reference;
    }
}

public class SubassemblyItemPackedCircuitData(int index) : DeferredPackedCircuitData(SubassemblyTrackerPackedCircuitData.MostRecentDecoded.IndexMap[index])
{
    public readonly int Index = index;

    public override byte[] Encode() =>
        [(byte)IPackedCircuitData.Mode.SubassemblyItem, .. BitConverter.GetBytes(Index)];

    public static IPackedCircuitData Decode(ByteReader reader) =>
        SubassemblyTrackerPackedCircuitData.MostRecentDecoded?.IndexMap.GetValueOrDefault(reader.ReadInt32());
}
