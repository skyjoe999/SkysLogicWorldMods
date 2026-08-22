using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using SkysCompactCircuits.Client.Addons;
using SkysCompactCircuits.Shared;
using UnityEngine;

namespace SkysCompactCircuits.Client.ClientCode;

public class PackedCircuitPrefabGenerator : DynamicPrefabGenerator<Prefab>
{

    public override (int inputCount, int outputCount) GetDefaultPegCounts() => (0, 0);
    protected override Prefab GeneratePrefabFor(Prefab identifier) => identifier;
    protected static Prefab GeneratePrefabFor(IPackedCircuitData identifier) =>
        identifier?.ComponentPrefab.Transform(scale: Vector3.one * identifier.TransformScale).Join(PartialPrefabsFor(identifier));
    protected override Prefab GetIdentifierFor(ComponentData componentData)
    {
        var circuit = (componentData.CustomData is not null && componentData.CustomData.Length != 0 && componentData.CustomData[0] != 0)
            ? PackedCircuitManager.Decode(componentData.CustomData) : null;
        return GeneratePrefabFor(circuit) ?? DefaultData;
    }
    public static readonly Prefab DefaultData =  new() { Blocks = [new() { RawColor = new(0xb32ec8) }] };


    public static Prefab PartialPrefabsFor(IPackedCircuitData data) =>
        PartialPrefabsFor(data.PartialWorld, data.AddonAddresses).Join().Transform(position: data.TransformOffset, Quaternion.Euler(data.TransformRotation), scale: Vector3.one * data.TransformScale);
    public static IEnumerable<Prefab> PartialPrefabsFor(PartialWorldData world, ComponentAddress[] addons)
    {
        foreach (var (generator, position, rotation) in ClientAddonManager.TransformsAndGeneratorsFor(world, addons))
            yield return new Prefab { Blocks = generator.GenerateBlocks() }.Transform(position, rotation);
    }
}

public class PackedCircuitPlacingRulesGenerator : DynamicPlacingRulesGenerator<Vector2Int>
{
    private static readonly Vector2[] HalfSteps = [.. Enumerable.Range(0, 3).SelectMany(x => Enumerable.Range(0, 3).Select(y => new Vector2(x / 2f, y / 2f)))];
    protected override PlacingRules GeneratePlacingRulesFor(Vector2Int identifier)
    {
        return new()
        {
            AllowWorldRotation = false,
            OffsetDimensions = identifier,
            SecondaryGridPositions = HalfSteps,
        };
    }

    protected override Vector2Int GetIdentifierFor(ComponentData componentData)
    {
        return (componentData.CustomData is not null && componentData.CustomData.Length != 0 && componentData.CustomData[0] != 0)
            ? PackedCircuitManager.Decode(componentData.CustomData)?.Size ?? Vector2Int.one : Vector2Int.one;
    }
}
