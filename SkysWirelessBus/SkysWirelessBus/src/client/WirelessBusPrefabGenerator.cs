using System.Collections.Generic;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using SkysWirelessBus.Shared;
using UnityEngine;

namespace SkysWirelessBus.Client.ClientCode.DynamicGenerators;
public class WirelessBusPrefabGenerator : DynamicPrefabGenerator<int>
{
    private static readonly Color24 BlockColor = new(50, 220, 180);

    protected override int GetIdentifierFor(ComponentData componentData)
    {
        return componentData.InputCount;
    }

    public override (int inputCount, int outputCount) GetDefaultPegCounts()
    {
        return (inputCount: IWirelessBusData.WirelessBusDefaultInputs, outputCount: 0);
    }

    protected override Prefab GeneratePrefabFor(int inputCount)
    {
        Block prefabBlock = new()
        {
            RawColor = BlockColor,
            Scale = new Vector3(inputCount / 3f, 0.6666666f, 0.6666666f),
            Position = new Vector3(inputCount / 6f - 0.1666666f, 0f, 0.1666666f)
        };

        List<ComponentInput> prefabInputs = [];

        for (int i = 0; i < inputCount; i++)
        {
            prefabInputs.Add(new ComponentInput
            {
                Position = new Vector3(i / 3f, 0.5f, -0.1666666f),
                Rotation = new Vector3(-90f, 0f, 0f),
                Length = 0.62f
            });
        }

        return new Prefab
        {
            Blocks = [prefabBlock],
            Outputs = [],
            Inputs = prefabInputs.ToArray()
        };
        
        
    }
}

