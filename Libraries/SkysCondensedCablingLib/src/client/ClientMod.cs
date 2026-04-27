using System.Collections.Generic;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld.Building;
using LogicWorld.Building.Overhaul.Grabbing;
using LogicWorld.Interfaces;
using LogicWorld.Networking.Handlers;
using LogicWorld.Rendering;
using LogicWorld.SharedCode;
using SkysCondensedCablingLib.Shared;
using SkysGeneralLib.Client.TypeExtensions;
using SkysGeneralLib.Shared.Networking;
using static LogicWorld.Building.Overhaul.Grabbing.StuffGrabber;

namespace SkysCondensedCablingLib.Client;

public class SkysCondensedCablingLib_ClientMod : ClientMod
{
    public override void Initialize()
    {
        new Harmony(Manifest.ID).PatchAll();

        FuncPacketHandler<UpdateSuperClusterPacket>.Add(static packet => SetupCluster(packet.StateID, packet.Color, packet.ConnectionID));
        FuncPacketHandler<BulkSuperClusterPacket>.Add(static packet =>
        {
            SuperWireBlocker.ClusterConnectionID.Clear();
            SuperColorTracker.ClusterColors.Clear();
            foreach (var (stateID, color, connectionID) in packet.values)
                SetupCluster(stateID, color, connectionID);
        });

        // In case this server doesn't support the mod and doesn't send a BulkSuperClusterPacket
        WorldInitializationHandler.OnPacketReceived += (_) =>
        {
            SuperWireBlocker.ClusterConnectionID.Clear();
            SuperColorTracker.ClusterColors.Clear();
        };
    }
    public static void SetupCluster(int stateID, (Color24 off, Color24 on)? color, int? connectionID)
    {
        if (color is { } _color)
            SuperColorTracker.ClusterColors[stateID] = _color;
        else
            SuperColorTracker.ClusterColors.Remove(stateID);
        if (connectionID is { } _connectionID)
            SuperWireBlocker.ClusterConnectionID[stateID] = _connectionID;
        else
            SuperWireBlocker.ClusterConnectionID.Remove(stateID);
        if (stateID >= 0)
            Instances.MainWorld.CircuitStates.SetStateAt(stateID, Instances.MainWorld.CircuitStates.GetStateAt(stateID));
    }
}

[HarmonyPatch]
public class SuperWireBlocker
{
    public static readonly Dictionary<int, int> ClusterConnectionID = [];
    [HarmonyPatch(typeof(WireUtility), nameof(WireUtility.WireWouldBeValid))]
    [HarmonyPostfix]
    static void WireWouldBeValid(ref bool __result, PegAddress peg1, PegAddress peg2)
    {
        if (__result)
        {
            var id1 = ClusterConnectionID.GetValueOrDefault(peg1.GetStateID(), -1);
            var id2 = ClusterConnectionID.GetValueOrDefault(peg2.GetStateID(), -1);
            // Negative two is the "connect any" id. (Negative one is standard (vanilla).)
            __result = id1 == id2 || id1 == -2 || id2 == -2;
        }
    }
}
[HarmonyPatch]
public class SuperColorTracker
{
    public static readonly Dictionary<int, (Color24 off, Color24 on)> ClusterColors = [];

    [HarmonyPatch(typeof(Colors), nameof(Colors.CircuitColor))]
    [HarmonyPrefix]
    static bool CircuitColor(ref GpuColor __result, bool on)
    {
        if (CurrentStateId == -1 || !ClusterColors.TryGetValue(CurrentStateId, out var colors))
            return true;

        __result = (on ? colors.on : colors.off).ToGpuColor();
        return false;
    }

    public static int CurrentStateId = -1;
    public static bool Cloning = false;

    [HarmonyPatch(typeof(CircuitStatesManager), nameof(CircuitStatesManager.UpdateCircuitColorAtIndex))]
    [HarmonyPrefix] public static void UpdateCircuitColorAtIndexPrefix(int index, CircuitStatesManager __instance) => CurrentStateId = Cloning || __instance is FastCircuitStatesManager ? index : -1;

    [HarmonyPatch(typeof(CircuitStatesManager), nameof(CircuitStatesManager.AddEntity))]
    [HarmonyPrefix] public static void AddEntityPrefix(int index, CircuitStatesManager __instance) => CurrentStateId = Cloning || __instance is FastCircuitStatesManager ? index : -1;


    [HarmonyPatch(typeof(CircuitStatesManager), nameof(CircuitStatesManager.UpdateCircuitColorAtIndex))]
    [HarmonyPostfix] public static void UpdateCircuitColorAtIndexPostfix() => CurrentStateId = -1;

    [HarmonyPatch(typeof(CircuitStatesManager), nameof(CircuitStatesManager.AddEntity))]
    [HarmonyPostfix] public static void AddEntityPostfix() => CurrentStateId = -1;


    [HarmonyPatch(typeof(GrabbingManager_Cloning), nameof(GrabbingManager_Cloning.StartCloning))]
    [HarmonyPrefix] public static void StartCloning() => Cloning = true;

    [HarmonyPatch(typeof(GrabbingState), nameof(GrabbingState.OnExit))]
    [HarmonyPrefix] public static void DisposeCloning() => Cloning = false;
}
