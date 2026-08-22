using System.Linq;
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
        SubassemblyTracker = 3,
        SubassemblyItem = 4,
        Compressed = 5,
        Modified = 6,
    }

    public byte[] Encode();
    public int InputCount => ComponentPrefab.Inputs.Length;
    public int OutputCount => ComponentPrefab.Outputs.Length;

    public static void AcceptModes(Mode mode, params Mode[] options)
    {
        if (!options.Contains(mode))
            throw new($"Found unexpected mode {mode}. (Maybe try updating the mod?)");
    }
}
