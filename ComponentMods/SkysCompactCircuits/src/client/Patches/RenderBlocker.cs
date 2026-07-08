using System.Collections.Generic;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Services;
using LogicWorld.Audio;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Chunks;
using LogicWorld.SharedCode.Components;
using SkysGeneralLib.Shared.AccessTools;

namespace SkysCompactCircuits.Client.Patches;

[HarmonyPatch]
public static class RenderBlocker
{
    [HarmonyPatch(typeof(EntityManager), nameof(EntityManager.RenderComponent), [typeof(ComponentDataManager)])]
    [HarmonyPrefix]
    public static bool RenderComponent(EntityManager __instance, ComponentDataManager componentDataManager)
    {
        var world = WorldDataAccess.Get(__instance);
        ComponentType packedCircuitType;
        try { packedCircuitType = ComponentTypesAccess.Get(__instance).GetComponentType(SkysCompactCircuits_ClientMod.PackedCircuitTextID); }
        catch (KeyNotFoundException) { return true; }

        if (!IsInsidePacked(componentDataManager.Data.Parent, world, packedCircuitType))
            return true;
        BlockEntitiesAccess.Get(EntitiesAccess.Get(__instance)).Add(componentDataManager.Address, []);
        return false;
    }
    [HarmonyPatch(typeof(EntityManager), nameof(EntityManager.RenderWire))]
    [HarmonyPrefix]
    public static bool RenderWire(EntityManager __instance, WireAddress wAddress)
    {
        var world = WorldDataAccess.Get(__instance);
        ComponentType packedCircuitType;
        try { packedCircuitType = ComponentTypesAccess.Get(__instance).GetComponentType(SkysCompactCircuits_ClientMod.PackedCircuitTextID); }
        catch (KeyNotFoundException) { return true; }
        if (!IsInsidePacked(world.Lookup(world.Lookup(wAddress).Point1.ComponentAddress).Parent, world, packedCircuitType))
            return true;
        SkipSound = wAddress;
        return false;
    }

    public static bool IsInsidePacked(ComponentAddress address, IWorldData world, ComponentType packedCircuitType)
    {
        var component = world.Lookup(address);
        while (address.IsNotEmpty() && component.Data.Type != packedCircuitType)
        {
            address = component.Parent;
            component = world.Lookup(address);
        }
        return !address.IsEmpty();
    }

    private static readonly Accessor<EntityManager, EntityTracker> EntitiesAccess = new("Entities");
    private static readonly Accessor<EntityTracker, Dictionary<ComponentAddress, RenderedEntity[]>> BlockEntitiesAccess = new("BlockEntities");
    private static readonly Accessor<EntityManager, IWorldData> WorldDataAccess = new("WorldData");
    private static readonly Accessor<EntityManager, ComponentTypesManager> ComponentTypesAccess = new("ComponentTypes");


    // This causes a bunch of null references we need to ignore
    [HarmonyPatch("CircuitStatesManager", "AddEntity")]
    private class AddEntityFix { public static bool Prefix(IColorChanger colorChanger) => colorChanger is not null; }
    [HarmonyPatch(typeof(EntityManager), "RemoveEntity")]
    private class RemoveEntityFix { public static bool Prefix(RenderedEntity entity) => entity is not null; }
    [HarmonyPatch("CircuitStatesManager", "RemoveEntity")]
    private class RemoveEntityFix2 { public static bool Prefix(IColorChanger colorChanger) => colorChanger is not null; }
    // [HarmonyPatch("EntityCollidersUtilities", "GetCollidersOfComponent")]
    // private class GetCollidersOfComponentFix
    // {
    //     public static bool Prefix(object __instance, ComponentAddress cAddress, ref bool alsoGetChildColliders)
    //     {
    //         if (!alsoGetChildColliders) return true;
    //         var componentInWorld = (WorldDataGetter(__instance) as IWorldRenderer).Lookup(cAddress)
    //         return;
    //     }
    // }
    // private static readonly Func<object, object> WorldDataGetter = Types.findInAssembly(typeof(LogicWorld.Rendering.Colliders.Extents), "EntityCollidersUtilities")
    //     .GetProperty("WorldData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue;

    // We need... to stop the *sound* because otherwise the game will crash! (can you tell I'm loosing it?)
    public static WireAddress? SkipSound = null;
    [HarmonyPatch(typeof(SoundPlayer), nameof(SoundPlayer.PlaySoundAt), [typeof(SoundEffect), typeof(WireAddress)])]
    [HarmonyPrefix]
    public static bool PlaySoundAt(WireAddress wAddress)
    {
        if (SkipSound != wAddress)
            return true;
        SkipSound = null;
        return false;
    }
}
