using System.Collections.Generic;
using System.Linq;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysCompactCircuits.Shared;

public static class PrefabExtensions
{
    public static Prefab Join(this IEnumerable<Prefab> prefabs)
    {
        prefabs = [.. prefabs];
        return new()
        {
            Inputs = [.. prefabs.SelectMany(p => p.Inputs)],
            Outputs = [.. prefabs.SelectMany(p => p.Outputs)],
            Blocks = [.. prefabs.SelectMany(p => p.Blocks)],
        };
    }
    public static Prefab Join(this Prefab prefab, Prefab other)
    {
        return new()
        {
            Inputs = [.. prefab.Inputs.Concat(other.Inputs)],
            Outputs = [.. prefab.Outputs.Concat(other.Outputs)],
            Blocks = [.. prefab.Blocks.Concat(other.Blocks)],
        };
    }
    public static Prefab Transform(this Prefab prefab, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        var _position = position ?? Vector3.zero;
        var _rotation = rotation ?? Quaternion.identity;
        var _scaleFactor = scale ?? Vector3.one;
        return new()
        {
            Inputs = [..prefab.Inputs.Select(i => new ComponentInput() {
                StartOn = i.StartOn,
                Length = i.Length,
                WirePointHeight = i.WirePointHeight,
                Bottomless = i.Bottomless,
                Position = _position + _rotation * Vector3.Scale(i.Position, _scaleFactor),
                Rotation = (_rotation * Quaternion.Euler(i.Rotation)).eulerAngles,
            })],
            Outputs = [..prefab.Outputs.Select(i => new ComponentOutput() {
                StartOn = i.StartOn,
                Bottomless = i.Bottomless,
                Position = _position + _rotation * Vector3.Scale(i.Position, _scaleFactor),
                Rotation = (_rotation * Quaternion.Euler(i.Rotation)).eulerAngles,
            })],
            Blocks = [..prefab.Blocks.Select(i => new Block() {
                MeshName = i.MeshName,
                Position = _position + _rotation * Vector3.Scale(i.Position, _scaleFactor),
                Rotation = (_rotation * Quaternion.Euler(i.Rotation)).eulerAngles,
                Scale = Vector3.Scale(i.Scale, _scaleFactor),
                ShouldBeOutlined = i.ShouldBeOutlined,
                RawColor = i.RawColor,
                Material = i.Material,
                ColliderData = i.ColliderData,
            })],
        };
    }

    public static IByteWriter Write(this IByteWriter writer, Prefab prefab)
    {
        writer = writer.Write(prefab.Blocks.Length);
        foreach (var block in prefab.Blocks)
            writer = writer.Write(block);
        writer = writer.Write(prefab.Inputs.Length);
        foreach (var input in prefab.Inputs)
            writer = writer.Write(input);
        writer = writer.Write(prefab.Outputs.Length);
        foreach (var output in prefab.Outputs)
            writer = writer.Write(output);
        return writer;
    }
    public static IByteWriter Write(this IByteWriter writer, Block block)
    {
        return writer
            .Write(block.MeshName)
            .Write(block.Position)
            .Write(block.Rotation)
            .Write(block.Scale)
            .Write(block.ShouldBeOutlined)
            .Write(block.RawColor)
            .Write((byte)block.Material)
            .Write(block.ColliderData);
    }
    public static IByteWriter Write(this IByteWriter writer, ComponentInput input)
    {
        return writer
            .Write(input.StartOn)
            .Write(input.Length)
            .Write(input.WirePointHeight)
            .Write(input.Bottomless)
            .Write(input.Position)
            .Write(input.Rotation)
            .Write(input.ColliderData);
    }
    public static IByteWriter Write(this IByteWriter writer, ComponentOutput output)
    {
        return writer
            .Write(output.StartOn)
            .Write(output.Bottomless)
            .Write(output.Position)
            .Write(output.Rotation)
            .Write(output.ColliderData);
    }
    public static IByteWriter Write(this IByteWriter writer, ColliderData data)
    {
        return writer
            .Write((byte)data.Type)
            .Write((byte)data.Layer)
            .Write(data.Transform.LocalPosition)
            .Write(data.Transform.LocalRotation)
            .Write(data.Transform.LocalScale);
    }

    public static Prefab ReadPrefab(this ByteReader reader)
    {
        return new()
        {
            Blocks = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadBlock())],
            Inputs = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadComponentInput())],
            Outputs = [.. Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadComponentOutput())],
        };
    }
    public static Block ReadBlock(this ByteReader reader)
    {
        return new()
        {
            MeshName = reader.ReadString(),
            Position = reader.ReadVector3(),
            Rotation = reader.ReadVector3(),
            Scale = reader.ReadVector3(),
            ShouldBeOutlined = reader.ReadByte() != 0,
            RawColor = reader.ReadColor24(),
            Material = (MaterialType)reader.ReadByte(),
            ColliderData = reader.ReadColliderData(),
        };
    }
    public static ComponentInput ReadComponentInput(this ByteReader reader)
    {
        return new()
        {
            StartOn = reader.ReadByte() != 0,
            Length = reader.ReadFloat(),
            WirePointHeight = reader.ReadFloat(),
            Bottomless = reader.ReadByte() != 0,
            Position = reader.ReadVector3(),
            Rotation = reader.ReadVector3(),
            ColliderData = reader.ReadColliderData(),
        };
    }
    public static ComponentOutput ReadComponentOutput(this ByteReader reader)
    {
        return new()
        {
            StartOn = reader.ReadByte() != 0,
            Bottomless = reader.ReadByte() != 0,
            Position = reader.ReadVector3(),
            Rotation = reader.ReadVector3(),
            ColliderData = reader.ReadColliderData(),
        };
    }
    public static ColliderData ReadColliderData(this ByteReader reader)
    {
        return new()
        {
            Type = (ColliderType)reader.ReadByte(),
            Layer = (ColliderLayer)reader.ReadByte(),
            Transform =
            {
                LocalPosition = reader.ReadVector3(),
                LocalRotation = reader.ReadVector3(),
                LocalScale = reader.ReadVector3(),
            },
        };
    }
}
