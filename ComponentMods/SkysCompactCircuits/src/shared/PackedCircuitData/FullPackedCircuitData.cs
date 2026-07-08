using System.Linq;
using LogicAPI.Data;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Components;
using LogicWorld.SharedCode.Modding;
using LogicWorld.SharedCode.PartialWorlds;
using UnityEngine;

namespace SkysCompactCircuits.Shared;

public class FullPackedCircuitData() : IPackedCircuitData
{
    public Prefab ComponentPrefab { set; get; }
    public PartialWorldData PartialWorld { set; get; }
    public ComponentAddress[] AddonAddresses { set; get; }
    public Vector3 TransformOffset { set; get; }
    public Vector3 TransformRotation { set; get; }
    public float TransformScale { set; get; } = 1;
    public Vector2Int Size { set; get; } = Vector2Int.one;
    public string Name { set; get; } = "";

    public byte[] Encode()
    {
        ByteWriter writer = new();
        writer.WriteObject(IPackedCircuitData.Mode.Full)
            .Write(Name)
            .Write(PartialWorldUtilities.Serialize(PartialWorld, ModRegistry.LoadedMods))
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
        return new FullPackedCircuitData
        {
            Name = reader.ReadString(),
            PartialWorld = PartialWorldUtilities.Deserialize(reader.ReadByteArray()),
            ComponentPrefab = reader.ReadPrefab(),
            TransformOffset = reader.ReadVector3(),
            TransformRotation = reader.ReadVector3(),
            TransformScale = reader.ReadFloat(),
            Size = reader.ReadVector2Int(),
            AddonAddresses = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadComponentAddress())],
        };
    }

    public FullPackedCircuitData(IPackedCircuitData copy) : this()
    {
        ComponentPrefab = copy.ComponentPrefab;
        PartialWorld = copy.PartialWorld;
        AddonAddresses = copy.AddonAddresses;
        TransformOffset = copy.TransformOffset;
        TransformRotation = copy.TransformRotation;
        TransformScale = copy.TransformScale;
        Size = copy.Size;
        Name = copy.Name;
    }
}
