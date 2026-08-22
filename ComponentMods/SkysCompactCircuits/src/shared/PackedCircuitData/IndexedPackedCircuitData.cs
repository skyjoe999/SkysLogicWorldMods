using LogicWorld.SharedCode.BinaryStuff;

namespace SkysCompactCircuits.Shared;

public class IndexedPackedCircuitData(int index) : DeferredPackedCircuitData(PackedCircuitManager.LookupIndexed(index))
{
    public readonly int Index = index;

    public override byte[] Encode()
    {
        ByteWriter writer = new();
        writer.WriteObject(IPackedCircuitData.Mode.Indexed).Write(Index); // all this just for type safety </3
        return writer.Finish();
    }

    public static IndexedPackedCircuitData Decode(ByteReader reader) =>
        new IndexedPackedCircuitData(reader.ReadInt32()) is { Reference: not null } indexed ? indexed : null;
}
