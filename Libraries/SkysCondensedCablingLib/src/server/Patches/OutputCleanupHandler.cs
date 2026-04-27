using HarmonyLib;
using LogicAPI;
using LogicAPI.Data;
using LogicWorld.Server.Circuitry;

namespace SkysCondensedCablingLib.Server;

[HarmonyPatch]
public static class OutputCleanupHandler
{
    [HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.FullyRemovePegsFromCircuitModel))]
    [HarmonyPrefix] public static void FullyRemovePegsFromCircuitModel(CircuitryManager __instance, ComponentAddress cAddress) => CleanupOutputsFor(__instance, cAddress);

    [HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.RemoveComponentFromCircuitModel))]
    [HarmonyPrefix] public static void RemoveComponentFromCircuitModel(CircuitryManager __instance, ComponentAddress cAddress) => CleanupOutputsFor(__instance, cAddress);


    public static void CleanupOutputsFor(CircuitryManager __instance, ComponentAddress cAddress)
    {
        foreach (var oAddress in __instance.WorldData.GetOutputAddressesOn(cAddress))
            if (__instance.LookupOutput(oAddress) is SuperOutputPeg output)
                SuperClusterClientHandler.SendCleanup(output);
    }
}
