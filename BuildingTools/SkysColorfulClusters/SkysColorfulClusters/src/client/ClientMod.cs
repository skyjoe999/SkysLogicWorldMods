using System;
using EccsLogicWorldAPI.Client.Hooks;
using FancyInput;
using HarmonyLib;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld;
using LogicWorld.GameStates;
using LogicWorld.Interfaces;
using LogicWorld.Networking.Handlers;
using LogicWorld.SharedCode;
using SkysColorfulClusters.Client.Keybindings;
using SkysColorfulClusters.Shared;
using SkysColorfulClusters.Shared.Packets;
using SkysGeneralLib.Shared.Networking;

namespace SkysColorfulClusters.Client;

public class SkysColorfulClusters_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        CustomInput.Register<SkysColorfulClustersContext, SkysColorfulClustersTrigger>("SkysColorfulClusters");
        // Harmony.DEBUG = true;
        new Harmony("SkysColorfulClustersClient").PatchAll();

        // triggers one of the patches to update the color
        ClusterColorManager.ClusterColorSet += (stateID, _) => RefreshCircuitState(stateID);


        FuncPacketHandler<ChangeClusterColorPacket>.Add(packet =>
        {
            foreach ((var stateID, var entry) in packet.clusterColors)
                ClusterColorManager.SetClusterColorUnsafe(stateID, entry);
        });

        FuncPacketHandler<ForceResetClusterColorsPacket>.Add(_ => Refresh());

        // Load all existing colors on world load (each world load)
        _ = SceneAndNetworkManager.MainWorld; // Runs the static init method (needed for the next line to not error out later)
        WorldInitializationHandler.OnPacketReceived += _ => ClusterColorExtraDataManager.SetupExtraData(Instances.MainWorld.ExtraData);

        FirstPersonInteraction.RegisterBuildingKeybinding(
            SkysColorfulClustersTrigger.EditWireColor,
            () =>
            {
                EditWireColor.TrySetEditingPeg();
                GameStateManager.TransitionTo(EditWireColor.GameStateTextID);
            }
        );
        WorldHook.worldLoading += () =>
        {
            //This action is in Unity execution scope, errors must be caught manually:
            try { EditWireColor.Build(); }
            catch (Exception e)
            {
                Logger.Error($"Failed to initialize GUI for {Manifest.Name}:");
                SceneAndNetworkManager.TriggerErrorScreen(e);
            }
        };
    }

    // [Command("ColorfulClusters.SetTargetColor", Description = "Brings up the GUI to recolor the targeted wire or peg.")]
    // private static void SetTargetColor()
    // {
    //     if (EditWireColor.TrySetEditingPeg())
    //         GameStateManager.TransitionTo(EditWireColor.GameStateTextID);
    //     else
    //         LConsole.WriteLine("Not looking at a peg or wire");
    // }
    public static void RefreshCircuitState(int stateID)
    {
        if (stateID >= 0)
            Instances.MainWorld.CircuitStates.SetStateAt(stateID, Instances.MainWorld.CircuitStates.GetStateAt(stateID));
    }

    [Command("ColorfulClusters.Refresh", Description = "Reloads all the cluster color data from the server")]
    public static void Refresh()
    {
        ClusterColorManager.ClearAll().ForEach(RefreshCircuitState);
        ClusterColorExtraDataManager.ReadFromExtraData();
    }
}



#region Patches
[HarmonyPatch(typeof(Colors), nameof(Colors.CircuitColor))]
static class ColorGetterOverride
{
    static int CurrentStateId = -1;
    static bool Prefix(out GpuColor __result, bool on)
    {
        var color = CurrentStateId != -1 ? ClusterColorManager.GetClusterColor(CurrentStateId, on) : null;
        __result = (color ?? default).ToGpuColor();
        return color is null;
    }
    [HarmonyPatch("CircuitStatesManager", "UpdateCircuitColorAtIndex")]
    static class ClusterColorTracker
    {
        static void Prefix(int index) => CurrentStateId = index;
        static void Postfix() => CurrentStateId = -1;
    }
    [HarmonyPatch("CircuitStatesManager", "AddEntity")]
    static class ClusterColorTracker2
    {
        static void Prefix(int index) => CurrentStateId = index;
        static void Postfix() => CurrentStateId = -1;
    }
}


#endregion
