using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Interfaces;
using LogicWorld.SharedCode;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysSockets.Client;

public interface IVirtualSocketHolder : IComponentClientCode
{
    public static readonly Vector2 DefaultBlueSquareScale = new Vector2(0.2f, 0.2f);
    public static readonly Vector2 ChubbyBlueSquareScale = new Vector2(0.8f, 0.8f);

    // Seperated incase the blocks are in a differnt order
    IEnumerable<int> BlockIndicies { get; }
    IEnumerable<int> InputIndicies { get; }
    void SetBlockColor(GpuColor color, int blockIndex = 0);

    public void frameUpdate()
    {
        foreach (var (iblock, iinput) in BlockIndicies.Zip(InputIndicies, (a, b) => (a, b)))
            SetBlockColor(GetInputState(iinput) ? Colors.SnappingPegOn : Colors.SnappingPegOff, iblock);
    }

    public static List<Block> GenerateSockets(
        IReadOnlyList<Vector3> relativePositions,
        IReadOnlyList<Quaternion> relativeRotations,
        IReadOnlyList<Vector2> blueSquareScales,
        Vector3 SmallOfset)
    {
        List<Block> _out = [];
        for (var i = 0; i < relativePositions.Count; i++)
        {
            _out.Add(new Block()
            {
                Position = relativePositions[i] + SmallOfset,
                Rotation = relativeRotations[i].eulerAngles,
                MeshName = "FlatQuad",
                Scale = new Vector3(blueSquareScales[i].x, 1f, blueSquareScales[i].y),
                ColliderData = new ColliderData() { Type = ColliderType.None },
                RawColor = new Color24( 0,  150,  141)
            });
        }

        return _out;
    }

    public static List<Block> GenerateSockets(
        IReadOnlyList<Vector3> relativePositions,
        IReadOnlyList<Quaternion> relativeRotations,
        Vector2 blueSquareScale,
        Vector3 SmallOfset)
    {
        return GenerateSockets(
            relativePositions,
            relativeRotations,
            Enumerable.Repeat(blueSquareScale, relativePositions.Count).ToList(),
            SmallOfset
        );
    }

    public static List<Block> GenerateSockets(
        IReadOnlyList<Vector3> relativePositions,
        IReadOnlyList<Quaternion> relativeRotations,
        Vector2 blueSquareScale)
    {
        return GenerateSockets(
            relativePositions,
            relativeRotations,
            blueSquareScale,
            Vector3.zero
        );
    }

    public static List<Block> GenerateSockets(
        IReadOnlyList<Vector3> relativePositions,
        IReadOnlyList<Quaternion> relativeRotations,
        IReadOnlyList<Vector2> blueSquareScale)
    {
        return GenerateSockets(
            relativePositions,
            relativeRotations,
            blueSquareScale,
            Vector3.zero
        );
    }
}