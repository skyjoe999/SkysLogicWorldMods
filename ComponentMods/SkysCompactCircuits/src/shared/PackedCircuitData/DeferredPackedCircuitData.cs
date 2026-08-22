using LogicAPI.Data;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysCompactCircuits.Shared;

public abstract class DeferredPackedCircuitData(IPackedCircuitData reference) : IPackedCircuitData
{
    public Prefab ComponentPrefab => Reference.ComponentPrefab;
    public PartialWorldData PartialWorld => Reference.PartialWorld;
    public ComponentAddress[] AddonAddresses => Reference.AddonAddresses;
    public Vector3 TransformOffset => Reference.TransformOffset;
    public Vector3 TransformRotation => Reference.TransformRotation;
    public float TransformScale => Reference.TransformScale;
    public Vector2Int Size => Reference.Size;
    public string Name => Reference.Name;
    public IPackedCircuitData Reference = reference;

    public abstract byte[] Encode();
}
