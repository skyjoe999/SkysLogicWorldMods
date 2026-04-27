using System.Collections.Generic;
using System.Reflection;
using EccsLogicWorldAPI.Client.Hooks;
using HarmonyLib;
using LICC;
using LogicAPI;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicAPI.Services;
using LogicAPI.WorldDataMutations;
using LogicWorld.Building;
using LogicWorld.Interfaces;
using LogicWorld.SharedCode;
using LogicWorld.SharedCode.Components;

namespace SkysCondensedCabling.Client;

public class SkysCondensedCabling_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        // Prefab
        // foreach (var item in typeof(Prefab).GetMethods(BindingFlags.Instance | BindingFlags.Public))
        // {
        //     LConsole.WriteLine(item.Name);
        // }
        
        var harmony = new Harmony("SkysCondensedCablingClient");
        // Harmony.DEBUG = true;
        harmony.PatchAll();
    }
}

// public class In2 : Cluster
// {
//     public In2(int stateId, CircuitStates circuitStates, SelfUpdatingContainer<Cluster> container, IReadOnlyList<InputPeg> connectedInputs, IReadOnlyList<OutputPeg> connectedOutputs, IReadOnlyList<ILogicUpdatable> connectedUpdatables):
//         base(stateId, circuitStates, container, connectedInputs, connectedOutputs, connectedUpdatables)
//     {
//         CircuitStates
//     }
// }

#region recolor
[HarmonyPatch(typeof(WorldMutation_UpdateInputStateID), nameof(WorldMutation_UpdateInputStateID.ApplyMutation))]
class InputStateChange
{
    static void Prefix(WorldMutation_UpdateInputStateID __instance, IWorldDataMutator mutator)
    {
        if (Instances.MainWorld.Data != mutator.Data)
            return;
        LConsole.WriteLine($"Input changed {Instances.MainWorld.Data.Lookup(__instance.AddressOfTargetInput).StateID} => {__instance.NewStateID}");
        Recolor.Transfer(Instances.MainWorld.Data.Lookup(__instance.AddressOfTargetInput).StateID, __instance.NewStateID);
    }
}
[HarmonyPatch(typeof(WorldMutation_UpdateOutputStateID), nameof(WorldMutation_UpdateOutputStateID.ApplyMutation))]
class OutputStateChange
{
    static void Prefix(WorldMutation_UpdateOutputStateID __instance, IWorldDataMutator mutator)
    {
        if (Instances.MainWorld.Data != mutator.Data)
            return;
        LConsole.WriteLine($"Output changed {Instances.MainWorld.Data.Lookup(__instance.AddressOfTargetOutput).StateID} => {__instance.NewStateID}");
        Recolor.Transfer(Instances.MainWorld.Data.Lookup(__instance.AddressOfTargetOutput).StateID, __instance.NewStateID);
    }
}

[HarmonyPatch(typeof(Colors), nameof(Colors.CircuitColor))]
static class Recolor
{
    static Recolor() => WorldHook.worldLoading += () => tracked.Clear();
    public static int current = -1;
    static readonly Dictionary<int, int> tracked = [];
    static bool Prefix(out GpuColor __result, bool on)
    {
        __result = on ? new(0.3f, 0.3f, 0.8f) : new(0.3f * 0.8f, 0.3f * 0.8f, 0.8f * 0.8f);
        return current == -1 || tracked.GetValueOrDefault(current) <= 0;
    }
    public static void Register(int stateId)
    {
        LConsole.WriteLine($"Registering: {stateId}");
        tracked.TryAdd(stateId, 0);
        tracked[stateId] += 1;
    }
    public static void Unregister(int stateId)
    {
        LConsole.WriteLine($"Unregistering: {stateId}");
        tracked[stateId] -= 1;
    }
    public static void Transfer(int oldStateId, int newStateId)
    {
        // LConsole.WriteLine($"Transferring {oldStateId}({tracked.GetValueOrDefault(oldStateId)}-1) -> {newStateId}({tracked.GetValueOrDefault(newStateId)}+1)");
        if (oldStateId == newStateId || tracked.GetValueOrDefault(oldStateId) <= 0)
            return;
        Unregister(oldStateId);
        Register(newStateId);
    }
    [Command("ShowTracked")]
    private static void ShowTracked()
    {
        foreach ((var key, var count) in tracked)
            if (count != 0)
                LConsole.WriteLine($"{key}: {count}");
    }
}

[HarmonyPatch("CircuitStatesManager", "UpdateCircuitColorAtIndex")]
class RecolorTracker
{
    static void Prefix(int index) => Recolor.current = index;
    static void Postfix() => Recolor.current = -1;
}
[HarmonyPatch("CircuitStatesManager", "AddEntity")]
class RecolorTracker2
{
    static void Prefix(int index) => Recolor.current = index;
    static void Postfix() => Recolor.current = -1;
}
#endregion

interface IHasSuperPegs : IComponentClientCode
{
    bool IsInputSuper(int index);
    bool IsOutputSuper(int index);
    void Register()
    {
        for (var index = 0; index < InputCount; index++)
            if (IsInputSuper(index)) Recolor.Register(Component.Data.InputInfos[index].StateID);
        for (var index = 0; index < OutputCount; index++)
            if (IsOutputSuper(index)) Recolor.Register(Component.Data.OutputInfos[index].StateID);
    }
    void Unregister()
    {
        for (var index = 0; index < InputCount; index++)
            if (IsInputSuper(index)) Recolor.Unregister(Component.Data.InputInfos[index].StateID);
        for (var index = 0; index < OutputCount; index++)
            if (IsOutputSuper(index)) Recolor.Unregister(Component.Data.OutputInfos[index].StateID);
    }
}


[HarmonyPatch(typeof(WireUtility), nameof(WireUtility.WireWouldBeValid))]
class WirePreventer
{
    static void Postfix(ref bool __result, PegAddress peg1, PegAddress peg2)
    {
        if (__result)
            __result = IsPegMulti(peg1) == IsPegMulti(peg2);
    }

    private static bool IsPegMulti(PegAddress peg)
    {
        return Instances.MainWorld.Renderer.Entities.GetClientCode(peg.ComponentAddress) is IHasSuperPegs client && peg.PegType switch
        {
            PegType.Input => client.IsInputSuper(peg.PegIndex),
            PegType.Output => client.IsOutputSuper(peg.PegIndex),
            _ => false
        };
    }
}


// [HarmonyPatch(typeof(ComponentRegistry), nameof(ComponentRegistry.LoadComponentsFile))]
// class PrefabFixer
// {
//     static void Postfix(ref ComponentInfo[] __result, ReadableDataFile file)
//     {
//         foreach ((var text, var i) in file.GetTopLevelKeysInOrder().Select((t, i) => (t, i)))
//             file.Get<SuperComponentInfo>(text)?.StaticPrefab?.ApplyTo(__result[i].StaticPrefab);
//     }
//     private class SuperComponentInfo
//     {
//         [SaveThis(SaveAs = "prefab")] public SuperPrefab StaticPrefab = null;
//         public class SuperPrefab
//         {
//             [SaveThis(SaveAs = "inputs")] public SuperComponentInput[] Inputs = [];
//             [SaveThis(SaveAs = "outputs")] public SuperComponentOutput[] Outputs = [];
//             public void ApplyTo(Prefab prefab)
//             {
//                 prefab.Inputs = [.. Inputs.Select((p, i) => p.DataType is null ? prefab.Inputs[i] : p)];
//                 prefab.Outputs = [.. Outputs.Select((p, i) => p.DataType is null ? prefab.Outputs[i] : p)];
//             }
//         }
//     }
// }

// public class SuperComponentInput : ComponentInput
// {
//     [SaveThis(SaveAs = "dataType")]
//     public Type DataType = null;
// }
// public class SuperComponentOutput : ComponentOutput
// {
//     [SaveThis(SaveAs = "dataType")]
//     public Type DataType = null;
// }
