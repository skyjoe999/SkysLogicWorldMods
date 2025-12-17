using System.Collections.Generic;
using System.Reflection;
using EccsLogicWorldAPI.Shared.AccessHelper;
using HarmonyLib;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.Input;
using LogicWorld.Interfaces;
using LogicWorld.Physics;
using UnityEngine;

namespace SkysBetterBoardLib.Client;

public class SkysBetterBoardLib_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        var harmony = new Harmony("SkysBetterBoardLibClient");
        // Harmony.DEBUG = true;
        harmony.PatchAll();
    }
}

// ReSharper disable All
[HarmonyPatch(typeof(StuffPlacer), nameof(StuffPlacer.CanMoveOn))]
class CanMovePatch
{
    static bool Prefix(HitInfo info, out bool __result)
    {
        __result = false;
        if (info.cAddress == ComponentAddress.Empty)
            return true;
        IComponentClientCode clientCode = Instances.MainWorld.Renderer.Entities.GetClientCode(info.cAddress);
        if (clientCode is not ICircuitBoardSurface)
            return true;
        __result = (clientCode as ICircuitBoardSurface).CanMoveOn(info);
        return false;
    }
}

[HarmonyPatch(typeof(StuffPlacer), "MoveOnBoardFace")]
class MoveOnBoardFacePatch
{
    static bool Prefix(StuffPlacer __instance, HitInfo info, Transform transformToMove, CircuitBoard board)
    {
        if (board is not ICircuitBoardSurface)
            return true;
        Transform transform = info.Hit.transform;
        // Vector2Int vector2Int1 = new Vector2Int(board.Data.SizeX, board.Data.SizeZ);
        // Vector3 localSpace = board.Component.ToLocalSpace(info.WorldPoint);
        var colliderScale = info.Hit.collider.transform.localScale / 0.3f;
        var vector2Int1 = new Vector2Int(Mathf.FloorToInt(colliderScale.x), Mathf.FloorToInt(colliderScale.z));
        var localSpace = info.Hit.transform.InverseTransformPoint(info.Hit.point);
        localSpace.Scale(colliderScale);
        var height = colliderScale.y;

        Vector2Int vector2Int2 = new Vector2Int(Mathf.FloorToInt(localSpace.x), Mathf.FloorToInt(localSpace.z));
        vector2Int2.Clamp(Vector2Int.zero, vector2Int1 - Vector2Int.one);
        bool flag = localSpace.y > height / 2;
        transformToMove.up = flag ? transform.up : -transform.up;
        Vector2[] vector2Array = !Trigger.Mod.Held() || __instance.CurrentPlacingRules.SecondaryGridPositions == null
            ? __instance.CurrentPlacingRules.PrimaryGridPositions
            : __instance.CurrentPlacingRules.SecondaryGridPositions;
        List<Vector2> vector2List = new List<Vector2>();
        foreach (var point in vector2Array)
        {
            var _point = point;
            if (__instance.CurrentPlacingRules.GridPositionsAreRelative)
            {
                float num = Quaternion.Angle(GetUnroundedPreRotation(__instance, transform.up), transform.rotation) +
                            __instance.Info.RotationAboutUpVector;
                _point = point.RotateAbout(new Vector2(0.5f, 0.5f), num.RoundTo(90f));
            }

            vector2List.Add((Vector2)vector2Int2 + _point);
        }

        Vector2 a = new Vector2(localSpace.x, localSpace.z);
        Vector2 vector2 = Vector2.zero;
        float num1 = float.MaxValue;
        foreach (Vector2 b in vector2List)
        {
            float num2 = Vector2.Distance(a, b);
            if ((double)num2 < (double)num1)
            {
                vector2 = b;
                num1 = num2;
            }
        }

        Vector3 vector3 = new Vector3(vector2.x, flag ? height : 0.0f, vector2.y) * 0.3f;
        transformToMove.position = transform.position + transform.rotation * vector3;
        return false;
    }

    private static Quaternion GetUnroundedPreRotation(StuffPlacer __instance, Vector3 upwardsDirection)
        => (Quaternion)GetUnroundedPreRotationFunc.Invoke(__instance, [upwardsDirection]);

    private static MethodInfo GetUnroundedPreRotationFunc =
        Methods.getPrivate(typeof(StuffPlacer), "GetUnroundedPreRotation");
}

[HarmonyPatch(typeof(StuffPlacer), "MoveOnBoardEdge")]
class MoveOnBoardEdgePatch
{
    static bool Prefix(StuffPlacer __instance, HitInfo info, Transform transformToMove, CircuitBoard board)
    {
        LConsole.WriteLine("Hit Transform Local: " + board.Component.ToLocalSpace(info.WorldPoint));
        if (board is not ICircuitBoardSurface)
            return true;
        Transform transform = info.Hit.transform;
        // Vector2Int vector2Int1 = new Vector2Int(board.Data.SizeX, board.Data.SizeZ);
        // Vector3 localSpace = board.Component.ToLocalSpace(info.WorldPoint);
        var colliderScale = info.Hit.collider.transform.localScale / 0.3f;
        var vector2Int1 = new Vector2Int(Mathf.FloorToInt(colliderScale.x), Mathf.FloorToInt(colliderScale.z));
        var localSpace = info.Hit.transform.InverseTransformPoint(info.Hit.point);
        localSpace.Scale(colliderScale);
        var height = colliderScale.y;

        Vector2Int vector2Int2 = new Vector2Int(Mathf.FloorToInt(localSpace.x), Mathf.FloorToInt(localSpace.z));
        vector2Int2.Clamp(Vector2Int.zero, vector2Int1 - Vector2Int.one);
        Vector2[] vector2Array = !Trigger.Mod.Held() || __instance.CurrentPlacingRules.SecondaryEdgePositions == null
            ? __instance.CurrentPlacingRules.PrimaryEdgePositions
            : __instance.CurrentPlacingRules.SecondaryEdgePositions;
        bool beingParallelWith = info.WorldNormal.IsPrettyCloseToBeingParallelWith(transform.forward);
        // bool flag1 = vector2Int2.x == board.Data.SizeX - 1 && info.WorldNormal == transform.right;
        // bool flag2 = vector2Int2.y == board.Data.SizeZ - 1 && info.WorldNormal == transform.forward;
        bool flag1 = vector2Int2.x == vector2Int1.x - 1 && info.WorldNormal == transform.right;
        bool flag2 = vector2Int2.y == vector2Int1.y - 1 && info.WorldNormal == transform.forward;
        List<Vector3> vector3List = new List<Vector3>();
        foreach (Vector2 vector2 in vector2Array)
        {
            Vector3 vector3 = new Vector3((float)vector2Int2.x, vector2.y, (float)vector2Int2.y);
            vector3 = vector3 +
                      new Vector3(beingParallelWith ? vector2.x : 0.0f, 0.0f, beingParallelWith ? 0.0f : vector2.x) +
                      new Vector3(flag1 ? 1f : 0.0f, 0.0f, flag2 ? 1f : 0.0f) + new Vector3(0.0f, height / 2, 0.0f);
            vector3List.Add(vector3);
        }

        Vector3 vector3_1 = Vector3.zero;
        float num1 = float.MaxValue;
        foreach (Vector3 b in vector3List)
        {
            float num2 = Vector3.Distance(localSpace, b);
            if (num2 < num1)
            {
                vector3_1 = b;
                num1 = num2;
            }
        }

        Vector3 vector3_2 = vector3_1 * 0.3f;
        transformToMove.position = transform.position + transform.rotation * vector3_2;
        transformToMove.up = info.WorldNormal;
        return false;
    }
}
