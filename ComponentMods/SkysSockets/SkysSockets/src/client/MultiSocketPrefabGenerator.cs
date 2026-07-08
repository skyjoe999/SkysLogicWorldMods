using System.Collections.Generic;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysSockets.Client.ClientCode.DynamicGenerators;

public class MultiSocketPrefabGenerator : DynamicPrefabGenerator<int>
{
    private static readonly Color24 BlockColor = new(169, 169, 169);

    protected override int GetIdentifierFor(ComponentData componentData) => componentData.InputCount;

    public override (int inputCount, int outputCount) GetDefaultPegCounts()
        => (inputCount: Shared.MultiSocket.MultiSocketDefaultInputs, outputCount: 0);

    protected override Prefab GeneratePrefabFor(int inputCount)
    {
        // Thats some weird syntax but alright IDE, if you say so
        List<Block> prefabBlocks =
        [
            ..IVirtualSocketHolder.GenerateSockets(
                Shared.MultiSocket.GetSocketPositions(inputCount),
                Shared.MultiSocket.GetSocketRotations(inputCount),
                IVirtualSocketHolder.DefaultBlueSquareScale,
                new Vector3(0f, 0f, 0.001f)
            ),

            new()
            {
                RawColor = BlockColor,
                Scale = new Vector3(inputCount / 3f, 2f / 3f, 2f / 3f),
                Position = new Vector3(inputCount / 6f - 1f / 6f, 0f, 1f / 6f)
            }

        ];

        List<ComponentInput> prefabInputs = [];
        for (int i = 0; i < inputCount; i++)
            prefabInputs.Add(new ComponentInput
            {
                Position = new Vector3(i / 3f, 0.5f, -1f / 6f),
                Rotation = new Vector3(-90f, 0f, 0f),
                Length = 0.62f
            });
        return new Prefab
        {
            Blocks = prefabBlocks.ToArray(),
            Outputs = [],
            Inputs = prefabInputs.ToArray()
        };
    }
}

