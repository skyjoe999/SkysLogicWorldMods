using System;
using System.Collections.Generic;
using HarmonyLib;
using LogicAPI;
using LogicAPI.Client;
using LogicAPI.Services;
using LogicAPI.WorldDataMutations;

namespace SkysNoMoreOutputWires.Client;

[HarmonyPatch]
public class SkysNoMoreOutputWires_ClientMod : ClientMod
{
    protected override void Initialize() => new Harmony(Manifest.ID).PatchAll();


    [HarmonyPatch("ClientWorldDataMutator", "UpdateWireStateID")]
    [HarmonyPrefix]
    public static void UpdateWireStateIDPatch(object __instance, ref WorldMutation_UpdateWireStateID mutation)
    {
        if (
            GetWorldData(__instance) is { } world &&
            world.AllWires.GetValueOrDefault(mutation.AddressOfTargetWire) is { } wire &&
            (!wire.Point1.IsInputAddress() || !wire.Point2.IsInputAddress()) &&
            world.Lookup(wire.Point1.IsInputAddress() ? wire.Point1 : wire.Point2) is { } peg
        )
            mutation.NewStateID = peg.StateID;
    }

    // I dont want to add more mod dependencies so we're doing this from first principles!
    [HarmonyPatch("ClientWorldDataMutator", "WorldData", MethodType.Getter)]
    [HarmonyReversePatch] public static IWorldData GetWorldData(object obj) => throw new NotImplementedException("It's a stub");
}
