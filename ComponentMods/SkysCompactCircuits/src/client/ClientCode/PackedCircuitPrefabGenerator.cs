using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using SkysCompactCircuits.Client.Addons;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Shared.AccessTools;
using UnityEngine;

namespace SkysCompactCircuits.Client.ClientCode;

public class PackedCircuitPrefabGenerator : DynamicPrefabGenerator<IPackedCircuitData>
{
    public readonly Dictionary<IPackedCircuitData, Prefab> Cache;
    public PackedCircuitPrefabGenerator() => Cache = new Accessor<DynamicPrefabGenerator<IPackedCircuitData>, Dictionary<IPackedCircuitData, Prefab>>("Cache").Get(this);

    public override (int inputCount, int outputCount) GetDefaultPegCounts() => (0, 0);
    protected override Prefab GeneratePrefabFor(IPackedCircuitData identifier) =>
        identifier.ComponentPrefab.Transform(scale: Vector3.one * identifier.TransformScale).Join(PartialPrefabsFor(identifier));
    protected override IPackedCircuitData GetIdentifierFor(ComponentData componentData)
    {
        if (Cache.Count > 2) Cache.Clear(); // caching will not help us here
        return (componentData.CustomData is not null && componentData.CustomData.Length != 0 && componentData.CustomData[0] != 0)
            ? PackedCircuitManager.TryDecode(componentData.CustomData) ?? DefaultData : DefaultData;
    }
    public static readonly IPackedCircuitData DefaultData = new FullPackedCircuitData()
    {
        AddonAddresses = [],
        ComponentPrefab = new() { Blocks = [new() { RawColor = new(0xb32ec8) }] },
        Size = new(1, 1),
        TransformScale = 1,
        PartialWorld = new(new Dictionary<ushort, string>(), [], [], []),
    };


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
            ? PackedCircuitManager.TryDecode(componentData.CustomData)?.Size ?? Vector2Int.one : Vector2Int.one;
    }
}
