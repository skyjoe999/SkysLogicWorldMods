using LogicAPI.Data;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysCompactCircuits.Shared;

public interface IPackedCircuitData
{
    public Prefab ComponentPrefab { get; }
    public PartialWorldData PartialWorld { get; }
    public ComponentAddress[] AddonAddresses { get; }
    public Vector3 TransformOffset { get; }
    public Vector3 TransformRotation { get; }
    public float TransformScale { get; }
    public Vector2Int Size { get; }
    public string Name { get; }


    public enum Mode : byte // Making this a single byte makes decoding easier and when will I really need 256+ formats!
    {
        Error = 0,
        Full = 1,
        Indexed = 2,
    }
    public byte[] Encode();
    public int InputCount => ComponentPrefab.Inputs.Length;
    public int OutputCount => ComponentPrefab.Outputs.Length;
}
