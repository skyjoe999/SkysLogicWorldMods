using System.Linq;
using LogicAPI.Data;
using LogicWorld.SharedCode.BinaryStuff;

namespace SkysCompactCircuits.Shared;

public class ModifiedIndexedPackedCircuitData(int index) : IndexedPackedCircuitData(index)
{
    public (ComponentAddress referenced, ComponentAddress child)[] AddonMap;
    public int[] ToggledOutStates;
    public (ComponentAddress referenced, byte[] data)[] ChangedCustomDatas;

    public override byte[] Encode()
    {
        ByteWriter writer = new();
        writer.WriteObject(IPackedCircuitData.Mode.Modified).Write(Index);

        writer.Write(AddonMap.Length);
        foreach (var (referenced, child) in AddonMap)
            writer.Write(referenced).Write(child);

        writer.Write(ToggledOutStates.Length);
        foreach (var state in ToggledOutStates)
            writer.Write(state);

        writer.Write(ChangedCustomDatas.Length);
        foreach (var (referenced, data) in ChangedCustomDatas)
            writer.Write(referenced).Write(data);

        return writer.Finish();
    }

    public static new ModifiedIndexedPackedCircuitData Decode(ByteReader reader)
    {
        var result = new ModifiedIndexedPackedCircuitData(reader.ReadInt32())
        {
            AddonMap = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => (reader.ReadComponentAddress(), reader.ReadComponentAddress()))],
            ToggledOutStates = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadInt32())],
            ChangedCustomDatas = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => (reader.ReadComponentAddress(), reader.ReadByteArray()))],
        };
        return result.Reference is not null ? result : null;
    }
}
