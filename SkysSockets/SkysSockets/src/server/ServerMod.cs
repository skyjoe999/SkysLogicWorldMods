using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using LogicAPI.Server;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Server.Components;
using LogicWorld.LogicCode;
using LogicWorld.Server.Circuitry;

namespace SkysSockets.Server;

public class SkysSockets_ServerMod : ServerMod
{
    public static void FudgeInputs(LogicComponent component, IInputPeg peg)
        => Patch1.FudgeInputs(component, peg);

    public static void SetCodeInfoFloats(LogicComponent component, float[] floats)
        => Patch2.SetCodeInfoFloats(component, floats);

    public static void SetComponent(LogicComponent component, IComponentInWorld value)
        => Patch2.SetComponent(component, value);

    protected override void Initialize()
    {
        var harmony = new Harmony("SkysSockets");
        // Harmony.DEBUG = true;
        harmony.PatchAll();
        Logger.Info("Sky's Sockets Initialized");
    }
}

// ReSharper disable All
[HarmonyPatch]
public class Patch1
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CircuitryManager), "InitializePegsInCircuitModel")]
    public static void FudgeInputs(LogicComponent component, IInputPeg peg)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            List<CodeInstruction> outList = [];
            AddOp(OpCodes.Ldarg_0); // push component
            AddOp(OpCodes.Ldc_I4_1); // push 1
            var idx = list.FindIndex(item => item.opcode == OpCodes.Callvirt) + 2;
            outList.Add(list[idx++]); // 1 => push new arr[1]
            outList.Add(list[idx]); // arr, component => _inputs
            idx = list.FindIndex(item => item.opcode == OpCodes.Ldc_I4_0);
            list.RemoveRange(0, idx);
            idx = list.FindIndex(item => item.opcode == OpCodes.Brfalse_S) + 2;
            AddOp(OpCodes.Ldarg_0); // push component
            outList.Add(list[idx]); // component => push _inputs
            AddOp(OpCodes.Ldc_I4_0); // push 0
            AddOp(OpCodes.Ldarg_1); // push peg
            AddOp(OpCodes.Stelem_Ref); // peg, 0, _inputs => _inputs[0] = peg
            return outList.AsEnumerable();

            void AddOp(OpCode op) => outList.Add(new CodeInstruction(op));
        }

        // make compiler happy
        _ = Transpiler(null);
    }
}

[HarmonyPatch]
class Patch2
{
    // I think harmony has a simple way to do this
    // But I have a hammer, so these must be nails
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(LogicComponent), "CodeInfoFloats", MethodType.Setter)]
    public static void SetCodeInfoFloats(LogicComponent component, float[] floats)
        => throw new NotImplementedException("It's a stub");

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(LogicComponent), "Component", MethodType.Setter)]
    public static void SetComponent(LogicComponent component, IComponentInWorld value)
        => throw new NotImplementedException("It's a stub");
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Socket), nameof(Socket.OnComponentDestroyed))]
    public static void StopTracking(Socket socket)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
          => instructions.ToList().GetRange(0, 2);
        // make compiler happy
        _ = Transpiler(null);
    }
}
// Looking back I probably could have just used any other component for my IComponentInWorld shenanigans
// But I guess this works too
[HarmonyPatch(typeof(Socket), nameof(Socket.OnComponentMoved))]
class Patch3
{
    static bool Prefix(Socket __instance)
    {
        return __instance.Inputs != null && __instance.Inputs.Count != 0;
    }
}

[HarmonyPatch(typeof(Socket), nameof(Socket.OnComponentDestroyed))]
class Patch4
{
    static bool Prefix(Socket __instance)
    {
        if (__instance.Inputs != null && __instance.Inputs.Count != 0) 
            return true;
        Patch2.StopTracking(__instance);
        return false;
    }
}