using LogicAPI.Data;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysCompactCircuits.Shared;

public class IndexedPackedCircuitData(int index) : IPackedCircuitData
{
    public Prefab ComponentPrefab => Reference.ComponentPrefab;
    public PartialWorldData PartialWorld => Reference.PartialWorld;
    public ComponentAddress[] AddonAddresses => Reference.AddonAddresses;
    public Vector3 TransformOffset => Reference.TransformOffset;
    public Vector3 TransformRotation => Reference.TransformRotation;
    public float TransformScale => Reference.TransformScale;
    public Vector2Int Size => Reference.Size;
    public string Name => Reference.Name;
    public readonly int Index = index;
    public readonly IPackedCircuitData Reference = PackedCircuitManager.LookupIndexed(index);

    public byte[] Encode()
    {
        ByteWriter writer = new();
        writer.WriteObject(IPackedCircuitData.Mode.Indexed).Write(Index); // all this just for type safety </3
        return writer.Finish();
    }

    public static IndexedPackedCircuitData Decode(ByteReader reader) => new(reader.ReadInt32());
}
