using System.Linq;
using HarmonyLib;
using LogicWorld.Building.Subassemblies;
using SkysCompactCircuits.Shared;

namespace SkysCompactCircuits.Client;

[HarmonyPatch]
public static class SubassemblyFix
{
    public static bool DontConvertNext = false;

    [HarmonyPatch(typeof(SubassembliesManager), nameof(SubassembliesManager.CreateSubassemblyDataFromSelection))]
    [HarmonyPostfix]
    public static void ConvertSubassemblyData(ref SubassemblyData __result)
    {
        if (DontConvertNext)
        {
            DontConvertNext = false;
            return;
        }

        var world = __result.PartialWorldData;
        var circuits = SubassemblyPackingHelper.GetIndexedCircuitsFromPartialWorld(world).ToArray();

        if (circuits.Length == 0)
            return; // No indexed circuits found (those are the only ones we care about).

        var tracker = new SubassemblyTrackerPackedCircuitData(
            circuits
                .Select(pair => pair.circuitIndex)
                .ToHashSet()
                .ToDictionary(circuitIndex => circuitIndex, circuitIndex => (IPackedCircuitData)new IndexedPackedCircuitData(circuitIndex)),
            circuits[0].circuitIndex
        );
        SubassemblyTrackerPackedCircuitData.MostRecentDecoded = tracker;

        // The first indexed circuit gets special treatment because we need it to load before the rest.
        __result.PartialWorldData = SubassemblyPackingHelper.ReplaceCustomDatas(world, circuits.Select((circuit, i) =>
            (circuit.childIndex, i == 0 ? tracker.Encode() : new SubassemblyItemPackedCircuitData(circuits[i].circuitIndex).Encode())
        ));
    }
}
