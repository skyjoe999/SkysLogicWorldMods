using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysChallengeSystem.Client.ClientCode;

public class ChallengeBoardPrefabGenerator : DynamicPrefabGenerator<int>
{
    private static readonly Color24 BoarderColor = new(0x202020);

    protected override int GetIdentifierFor(ComponentData componentData)
    {
        return componentData.InputCount;
    }

    protected override Prefab GeneratePrefabFor(int identifier)
    {
        return new Prefab()
        {
            Blocks =
            [
                new Block
                {
                    Material = MaterialType.Board,
                    MeshName = "OriginCube",
                    Position = new Vector3(0.5f, 0, 0.5f),
                },
                new Block
                {
                    RawColor = BoarderColor,
                    MeshName = "OriginCube",
                    Position = new Vector3(0, 0, 0.5f),
                },
                new Block
                {
                    RawColor = BoarderColor,
                    MeshName = "OriginCube",
                },
                new Block
                {
                    RawColor = BoarderColor,
                    MeshName = "OriginCube",
                },
                new Block
                {
                    RawColor = BoarderColor,
                    MeshName = "OriginCube",
                },
            ],
            Inputs = new byte[identifier].Convert(_ => new ComponentInput
            {
                Position = new Vector3(0.5f, 0.25f, 0.5f),
                Length = 0.001f,
                ColliderData = new ColliderData { Type = ColliderType.None },
            }),
        };
    }

    public override (int inputCount, int outputCount) GetDefaultPegCounts()
    {
        return (0, 0);
    }
}