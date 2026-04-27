using HarmonyLib;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using SkysGeneralLib.Server;

namespace SkysCondensedCablingLib.Server;

[HarmonyPatch]
public static class AutoClusterConverter
{

    [HarmonyPatch(typeof(ClusterFactory), nameof(ClusterFactory.Create))]
    [HarmonyPostfix]
    public static void CreateOverride(ref Cluster __result) => SuperClusterClientHandler.SendSetupAny(__result = SuperClusterFactory.Create(__result));

    [HarmonyPatch(typeof(ClusterFactory), nameof(ClusterFactory.CreateStarter))]
    [HarmonyPostfix]
    public static void CreateStarterOverride(ref Cluster __result, InputPeg input) => SuperClusterClientHandler.SendSetupAny(__result = SuperClusterFactory.CreateStarter(__result, input));

    [HarmonyPatch(typeof(Cluster), nameof(Cluster.Destroy))]
    [HarmonyPostfix]
    public static void DestroyOverride(Cluster __instance) => SuperClusterClientHandler.SendCleanupAny(__instance);


    [HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.InitializePegsInCircuitModel))]
    [HarmonyPostfix]
    public static void PegInitializationOverride(LogicComponent logic, CircuitryManager __instance)
    {
        // logic may also be null but if so then it cannot have super pegs
        if (logic is not IHasSuperPegs super)
            return;

        for (var i = 0; i < logic._Inputs.Length; i++)
            if (super.InputSuperSize(i) > 0 && logic._Inputs[i] is InputPeg input)
            {
                var sInput = new SuperInputPeg(input);
                logic._Inputs[i] = __instance.LogicInputs[input.iAddress] = sInput;
                if (__instance.PegsToInitializeAtEndOfBatchClusterInitialization.Remove(input))
                    __instance.PegsToInitializeAtEndOfBatchClusterInitialization.Add(sInput);
                else
                {
                    input.Cluster.Destroy();
                    __instance.ClusterFactory.CreateStarter(sInput);
                }
            }

        for (var i = 0; i < logic._Outputs.Length; i++)
            if (super.OutputSuperSize(i) is > 0 and { } size && logic._Outputs[i] is OutputPeg output)
            {
                output.CircuitStates.FreeIndex(output.StateID); // by freeing it first we avoid having to send an update packet.
                var sOutput = new SuperOutputPeg(output, size, super.OutputFamily(i));
                logic._Outputs[i] = __instance.LogicOutputs[output.oAddress] = sOutput;
                SuperClusterClientHandler.SendSetup(sOutput);
            }
    }


    public static bool TryUpConvert(Cluster source, ref Cluster target)
    {
        if (source is not SuperCluster sSource || target is null || target.CircuitStates != Services.CircuitStates || ((target as SuperCluster)?.Size ?? 0) >= sSource.Size)
            return false;

        target = Services.IClusterFactory.Create([.. target.ConnectedInputs], [.. target.ConnectedOutputs]);
        if (target is not SuperCluster sTarget || sTarget.Size < sSource.Size)
            target = SuperClusterFactory.Create(target, sSource.Size);

        return true;
    }



    [HarmonyPatch(typeof(Cluster), nameof(Cluster.AddOneWayLinkTo))]
    [HarmonyPrefix]
    public static bool AddOneWayLinkTo(ref Cluster __instance, ref Cluster other)
    {
        if (TryUpConvert(__instance, ref other) || __instance is not SuperCluster sInst || other is not SuperCluster sOther)
            return true;

        sInst.AddSuperOneWayLinkTo(sOther);
        return false;
    }

    [HarmonyPatch(typeof(Cluster), nameof(Cluster.RemoveOneWayLinkTo))]
    [HarmonyPrefix]
    public static bool RemoveOneWayLinkTo(Cluster __instance, Cluster other)
    {
        if (__instance is not SuperCluster sInst || other is not SuperCluster sOther)
            return true;

        sInst.RemoveSuperOneWayLinkTo(sOther);
        return true;
    }

    [HarmonyPatch(typeof(Cluster), nameof(Cluster.AddTwoWayLinkWith))]
    [HarmonyPrefix]
    public static bool AddTwoWayLinkWith(ref Cluster __instance, ref Cluster other)
    {
        if (TryUpConvert(__instance, ref other))
            return false;
        if (TryUpConvert(other, ref __instance))
            return false;
        if (__instance is not SuperCluster sInst || other is not SuperCluster sOther)
            return true;

        sInst.AddSuperTwoWayLinkWith(sOther);
        return false;
    }

    [HarmonyPatch(typeof(Cluster), nameof(Cluster.RemoveTwoWayLinkWith))]
    [HarmonyPrefix]
    public static bool RemoveTwoWayLinkWith(Cluster __instance, Cluster other)
    {
        if (__instance is not SuperCluster sInst || other is not SuperCluster sOther)
            return true;

        sInst.RemoveSuperTwoWayLinkWith(sOther);
        return true;
    }
}