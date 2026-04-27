using System;
using HarmonyLib;
using LogicWorld.Server.Circuitry;

namespace SkysCondensedCablingLib.Server;

[HarmonyPatch]
public static class DummyInputManager
{
    [HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.FinalizeBatchClusterInitialization))]
    [HarmonyPostfix]
    public static void FixAll()
    {
        OnFinalize?.Invoke();
        OnFinalize = null;
    }

    private static event Action OnFinalize;
    public static void QueueLink(Action queueAction) => OnFinalize += queueAction;
}
