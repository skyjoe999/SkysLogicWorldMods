using System;
using System.Collections.Generic;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.WorldDataMutations;
using LogicLog;
using LogicWorld.SharedCode.PartialWorlds;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Server;

namespace SkysCompactCircuits.Server;

[HarmonyPatch]
public static class BuildOverride
{
    private static readonly ComponentType PackedCircuitType = Services.ComponentTypesManager.GetComponentType("SkysCompactCircuits.PackedCircuit");

    [HarmonyPatch("BuildAction_CreateSingleNewComponent", "EnumerateMutations")]
    [HarmonyPostfix]
    public static void EnumerateMutationsOverride(ref IEnumerable<WorldDataMutation> __result)
    {
        var mutations = __result;
        __result = Process();

        // a little overkill but it ensures mod compatibility (hopefully...)
        IEnumerable<WorldDataMutation> Process()
        {
            foreach (var mutation in mutations)
                if (mutation is WorldMutation_AddSingleNewComponent asc && asc.NewComponent.Type == PackedCircuitType)
                {
                    var data = PackedCircuitManager.DecodeAndIndex(asc.NewComponent.CustomData);
                    if (PackedCircuitStructureManager.TryGetAdditionWorldGuid(data, out var guid))
                    {
                        yield return new WorldMutation_AddPartialWorld()
                        {
                            AdditionStartingComponentAddressID = Services.IWorldDataMutator.HighestComponentAddressAddedSoFar + 1,
                            AdditionStartingWireAddressID = Services.IWorldDataMutator.HighestWireAddressAddedSoFar + 1,
                            GuidOfPartialWorldToAdd = guid,
                            WiresToExcludeByIndex = [],
                            RootAdditionInfos = [new()
                            {
                                AdditionLocalPosition = asc.NewComponent.LocalPosition,
                                AdditionLocalRotation = asc.NewComponent.LocalRotation,
                                AdditionParent = asc.NewComponent.Parent,
                            }],

                        };
                    }
                    else
                    {
                        // We should try to prevent this on the client but having a fall back is safer
                        LogicLogger.For("SkysCompactCircuits.BuildOverride").Error("Tried to place compact circuit that uses component types that are not loaded");
                        yield return GenerateFallbackMutation(data);
                    }
                }
                else
                    yield return mutation;
        }
    }

    [HarmonyPatch(typeof(PartialWorldCacheDatabase), nameof(PartialWorldCacheDatabase.TryGetPartialWorld))]
    [HarmonyPrefix]
    public static bool TryGetPartialWorldOverride(out bool __result, Guid guid, out PartialWorldData partialWorldData)
    {
        return !(__result = PackedCircuitStructureManager.TryGetAdditionWorld(guid, out partialWorldData));
    }
    [HarmonyPatch(typeof(PartialWorldCacheDatabase), nameof(PartialWorldCacheDatabase.ContainsPartialWorld))]
    [HarmonyPrefix]
    public static bool ContainsPartialWorldOverride(out bool __result, Guid guid)
    {
        return !(__result = PackedCircuitStructureManager.TryGetAdditionWorld(guid, out _));
    }

    public static WorldMutation_AddSingleNewComponent GenerateFallbackMutation(IPackedCircuitData data)
    {
        // There is no *good* solution... but I like this one...
        var first = data.PartialWorld.OrderedComponentsAndAddresses[0].componentData;
        var newAddress = new ComponentAddress(Services.IWorldDataMutator.HighestComponentAddressAddedSoFar + 1);

        var newComponent = (IEditableComponentData)new ComponentData(Services.ComponentTypesManager.GetComponentType("MHG.PanelLabel"));
        newComponent.InputInfos = [];
        newComponent.OutputInfos = [];
        newComponent.CustomData = FallbackData;
        newComponent.Parent = first.Parent;
        newComponent.LocalPositionFixed = first.LocalPositionFixed;
        newComponent.LocalRotation = first.LocalRotation;
        return new() { NewComponent = (ComponentData)newComponent, AddressOfNewComponent = newAddress };
    }

    // Ooooo, mysterious magic numbers! Aren't you just dying to find out what they mean???
    private static readonly byte[] FallbackData = [
        0x01, 0x00, 0x00, 0x00, 0x26, 0x26, 0x26, 0xCD, 0xCC, 0x4C, 0x3F, 0x00,
        0x4C, 0x00, 0x00, 0x00, 0x54, 0x68, 0x65, 0x20, 0x63, 0x69, 0x72, 0x63,
        0x75, 0x69, 0x74, 0x20, 0x79, 0x6F, 0x75, 0x20, 0x61, 0x72, 0x65, 0x20,
        0x74, 0x72, 0x79, 0x69, 0x6E, 0x67, 0x20, 0x74, 0x6F, 0x20, 0x70, 0x6C,
        0x61, 0x63, 0x65, 0x20, 0x75, 0x73, 0x65, 0x73, 0x20, 0x63, 0x6F, 0x6D,
        0x70, 0x6F, 0x6E, 0x65, 0x6E, 0x74, 0x73, 0x20, 0x74, 0x68, 0x61, 0x74,
        0x20, 0x61, 0x72, 0x65, 0x20, 0x6E, 0x6F, 0x74, 0x20, 0x72, 0x65, 0x67,
        0x65, 0x73, 0x74, 0x65, 0x72, 0x65, 0x64, 0x2E, 0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00
    ];

}
