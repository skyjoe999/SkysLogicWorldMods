using System;
using System.Collections.Generic;
using System.Reflection;
using EccsLogicWorldAPI.Shared.AccessHelper;
using FancyInput;
using HarmonyLib;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld.Building.Overhaul;
using LogicWorld.Input;
using LogicWorld.Physics;
using UnityEngine;

namespace SkysMetaphysicalThesisOnTheArbitrarinessOfPlacementRules.Client;

public class SkysMetaphysicalThesisOnTheArbitrarinessOfPlacementRules_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        var harmony = new Harmony("SkysMetaphysicalThesisOnTheArbitrarinessOfPlacementRulesClient");
        // Harmony.DEBUG = true;
        harmony.PatchAll();
    }
}

// ReSharper disable All
[HarmonyPatch(typeof(StuffPlacer), nameof(StuffPlacer.CanMoveOn))]
class Patch1
{
    static bool Prefix(HitInfo info, out bool __result)
    {
        // __result = false;
        // if (info.cAddress == ComponentAddress.Empty)
        //     return false;
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(StuffPlacer), "MoveGhostOn")]
class Patch2
{
    static bool Prefix(StuffPlacer __instance, HitInfo info)
    {
        Transform transform = __instance.Ghost.Transform;
        setMostRecentCollider(__instance.Ghost, info.Hit.collider);
        setPreviousMoveAddress(__instance.Ghost, info.cAddress);
        setPreviousMoveType(__instance.Ghost, StuffPlacer.MoveType.EnvironmentSmooth);
        transform.position = info.WorldPoint;
        transform.up = info.WorldNormal;
        return false;
    }

    private static Action<PlacingGhost, StuffPlacer.MoveType> setPreviousMoveType
        = Delegator.createPropertySetter<PlacingGhost, StuffPlacer.MoveType>(
            Properties.getPublic(typeof(PlacingGhost), "PreviousMoveType"));

    private static Action<PlacingGhost, Collider> setMostRecentCollider
        = Delegator.createPropertySetter<PlacingGhost, Collider>(
            Properties.getPublic(typeof(PlacingGhost), "MostRecentCollider"));

    private static Action<PlacingGhost, ComponentAddress> setPreviousMoveAddress
        = Delegator.createPropertySetter<PlacingGhost, ComponentAddress>(
            Properties.getPublic(typeof(PlacingGhost), "PreviousMoveAddress"));
}

[HarmonyPatch(typeof(StuffPlacer), "PollModifierInput")]
class Patch4
{
    static bool Prefix(StuffPlacer __instance)
    {
        // Trigger rot = Trigger.NextHotbarItem;
        if (Trigger.FineRotateClockwise.Held())
            GetDegreesToRotateBy(Trigger.FineRotateClockwise, __instance);
        else if (Trigger.FineRotateCounterclockwise.Held())
            GetDegreesToRotateBy(Trigger.FineRotateCounterclockwise, __instance);
        else if (Trigger.RotateClockwise.Held())
            GetDegreesToRotateBy(Trigger.RotateClockwise, __instance);
        else if (Trigger.RotateCounterclockwise.Held())
            GetDegreesToRotateBy(Trigger.RotateCounterclockwise, __instance);


        InputTrigger triggerDown = CustomInput.WhichIsDown<Context>(Context.PlacingSomething);
        if (
            triggerDown == Trigger.RotateClockwise ||
            triggerDown == Trigger.RotateCounterclockwise ||
            triggerDown == Trigger.FineRotateClockwise ||
            triggerDown == Trigger.FineRotateCounterclockwise
        )
        {
            return false;
        }

        return true;
    }

    private static void GetDegreesToRotateBy(Trigger trigger, StuffPlacer instance)
    {
        // TODO: where the hell is deltaTime???
        switch (trigger)
        {
            case Trigger.RotateClockwise:
                RotateGhost(instance, 3f);
                return;
            case Trigger.RotateCounterclockwise:
                RotateGhost(instance, -3f);
                return;
            case Trigger.FineRotateClockwise:
                RotateGhost(instance, 1f);
                return;
            case Trigger.FineRotateCounterclockwise:
                RotateGhost(instance, -1f);
                return;
        }
    }

    private static void RotateGhost(StuffPlacer ghost, float amount)
    {
        ghost.Info.RotationAboutUpVector += amount;
        ghost.Ghost.Transform.Rotate(Vector3.up, amount);
    }
}

[HarmonyPatch(typeof(StuffPlacer), "ApplyPostMovementModifiers")]
class Patch5
{
    static bool Prefix(StuffPlacer __instance)
    {
        // __instance.Ghost.Transform.rotation = __instance.GetUnroundedPreRotation(__instance.Ghost.Transform.up, true);
        // __instance.Ghost.Transform.Rotate(__instance.Ghost.Transform.up,
        //     __instance.ExtraSystemRotationWhileSmoothPlacing);
        ApplyRotation();
        ApplyOffset();
        setCornerMidpointPositionBeforeFlipping(__instance, __instance.Ghost.Transform.TransformPoint(
            new Vector3(__instance.CurrentPlacingRules.CornerMidpoint.x, 0.0f,
                __instance.CurrentPlacingRules.CornerMidpoint.y) *
            0.3f));
        setRotationBeforeFlipping(__instance, __instance.Ghost.Transform.rotation);
        ApplyFlippedness();

        void ApplyRotation()
        {
            __instance.Ghost.Transform.Translate(
                new Vector3(__instance.CurrentPlacingRules.CornerMidpoint.x, 0.0f,
                    __instance.CurrentPlacingRules.CornerMidpoint.y) * -0.3f, Space.Self);
            __instance.Ghost.Transform.RotateAround(
                __instance.Ghost.Transform.TransformPoint(new Vector3(__instance.CurrentPlacingRules.CornerMidpoint.x,
                    0.0f,
                    __instance.CurrentPlacingRules.CornerMidpoint.y) * 0.3f), __instance.Ghost.Transform.up,
                __instance.Info.RotationAboutUpVector);
        }

        void ApplyOffset()
        {
            Vector2 offsetScale = __instance.CurrentPlacingRules.OffsetScale;
            Vector2Int offset = __instance.Info.Offset;
            double x = (double)offset.x * (double)offsetScale.x;
            offset = __instance.Info.Offset;
            double z = (double)offset.y * (double)offsetScale.y;
            __instance.Ghost.Transform.Translate(new Vector3((float)x, 0.0f, (float)z) * -0.3f, Space.Self);
        }

        void ApplyFlippedness()
        {
            if (!__instance.Info.Flipped)
                return;
            Transform transform = __instance.Ghost.Transform;
            Vector3 position =
                (new Vector3((float)__instance.CurrentPlacingRules.OffsetDimensions.x / 2f,
                     __instance.CurrentPlacingRules.FlippingPointHeight,
                     (float)__instance.CurrentPlacingRules.OffsetDimensions.y / 2f) - new Vector3(0.5f, 0.0f, 0.5f) +
                 new Vector3(__instance.CurrentPlacingRules.CornerMidpoint.x, 0.0f,
                     __instance.CurrentPlacingRules.CornerMidpoint.y)) * 0.3f;
            Vector3 point = transform.TransformPoint(position);
            transform.RotateAround(point, transform.forward, 180f);
        }

        return false;
    }

    private static Action<StuffPlacer, Vector3> setCornerMidpointPositionBeforeFlipping
        = Delegator.createPropertySetter<StuffPlacer, Vector3>(
            Properties.getPublic(typeof(StuffPlacer), "CornerMidpointPositionBeforeFlipping"));

    private static Action<StuffPlacer, Quaternion> setRotationBeforeFlipping
        = Delegator.createPropertySetter<StuffPlacer, Quaternion>(
            Properties.getPublic(typeof(StuffPlacer), "RotationBeforeFlipping"));
}

[HarmonyPatch(typeof(StuffPlacer), "ValidatePlacingInfo")]
class Patch6
{
    static bool Prefix(StuffPlacer __instance)
    {
        // __instance.Info.Flipped = __instance.Info.Flipped && __instance.CurrentPlacingRules.CanBeFlipped;
        __instance.Info.Offset =
            __instance.Info.Offset.Clamped(Vector2Int.zero,
                __instance.CurrentPlacingRules.OffsetDimensions - Vector2Int.one);
        __instance.Info.RotationAboutUpVector = __instance.Info.RotationAboutUpVector.WrapRange(360f);
        return false;
    }
}

[HarmonyPatch]
class Patch7
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        return
        [
            Assemblies.findAssemblyWithName("LogicWorld.Building")
                .GetType("LogicWorld.Building.Overhaul.Grabbing.GrabbingManager_Moving")!
                .GetProperty("DoRotationUnlockingWhileHoldingMod", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetMethod,
            Assemblies.findAssemblyWithName("LogicWorld.Building")
                .GetType("LogicWorld.Building.Overhaul.Grabbing.GrabbingManager_Cloning")
                .GetProperty("DoRotationUnlockingWhileHoldingMod", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetMethod,
        ];
    }

    static bool Prefix(bool __result) => __result = false;
}

// [HarmonyPatch] //"IGrabbingManager", "OnRun")]
// class Patch3
// {
//     static IEnumerable<MethodBase> TargetMethods()
//     {
// //         IGrabbingManager
// // GrabbingManager`1
// // GrabbingManager_Cloning
// // GrabbingManager_ForComponentsInWorld
// // GrabbingManager_ForSubassembly
// // GrabbingManager_Moving
// // GrabbingManager_SingleHotbarItem
//         var result = new List<MethodBase>();
//         TypeInfo type = null;
//         foreach (var v in Assemblies.findAssemblyWithName("LogicWorld.Building").DefinedTypes)
//         {
//             FileLog.Log(v.Name);
//             if (v.Name != "GrabbingManager_ForComponentsInWorld") continue;
//             type = v;
//             // break;
//         }

//         FileLog.Log(type + " <---");
//         foreach (var v in type.BaseType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
//         {
//             FileLog.Log(v.Name);
//         }

//         // ... (targeting all DamageHandler.Apply derived)
//         return
//         [
//             type.BaseType
//                 // .MakeGenericType([typeof(ComponentAddress)])
//                 .GetMethod("LogicWorld.Building.Overhaul.Grabbing.IGrabbingManager.OnRun", BindingFlags.Instance | BindingFlags.NonPublic )
//         ];
//     }

//     static bool Prefix(object __instance)
//     {
//         // Transform transform = __instance.Ghost.Transform;
//         // setMostRecentCollider(__instance.Ghost, info.Hit.collider);
//         // setPreviousMoveAddress(__instance.Ghost, info.cAddress);
//         // setPreviousMoveType(__instance.Ghost, StuffPlacer.MoveType.EnvironmentSmooth);
//         // transform.position = info.WorldPoint;
//         // transform.up = info.WorldNormal;
//         StuffPlacer ActivePlacer = getActivePlacer(__instance);
//         if (Trigger.RotateClockwise.Held())
//         {
//             ActivePlacer.
//         }
//         return true;
//     }

//     private static Func<object, StuffPlacer> getActivePlacer = (object instance)
//         => _getActivePlacer(instance) as StuffPlacer;
//     private static Func<object, object> _getActivePlacer
//         = Assemblies.findAssemblyWithName("LogicWorld.Building")
//                 .GetType("IGrabbingManager")
//                 .GetField("ActivePlacer")
//                 .GetValue;


//     private static Action<PlacingGhost, Collider> setMostRecentCollider
//         = Delegator.createPropertySetter<PlacingGhost, Collider>(
//             Properties.getPublic(typeof(PlacingGhost), "MostRecentCollider"));

//     private static Action<PlacingGhost, ComponentAddress> setPreviousMoveAddress
//         = Delegator.createPropertySetter<PlacingGhost, ComponentAddress>(
//             Properties.getPublic(typeof(PlacingGhost), "PreviousMoveAddress"));
// }
