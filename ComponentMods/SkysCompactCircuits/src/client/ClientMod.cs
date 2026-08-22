using System;
using EccsLogicWorldAPI.Client.Hooks;
using FancyInput;
using HarmonyLib;
using LICC;
using LogicAPI.Client;
using LogicWorld;
using LogicWorld.SharedCode.Components;
using SkysCompactCircuits.Client.ClientCode;
using SkysCompactCircuits.Client.Gui;
using SkysCompactCircuits.Client.Keybindings;
using SkysCompactCircuits.Shared;
using SkysCompactCircuits.Shared.Packets;
using SkysGeneralLib.Shared.Networking;

namespace SkysCompactCircuits.Client;

public class SkysCompactCircuits_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        CustomInput.Register<SkysCompactCircuitsContext, SkysCompactCircuitsTrigger>(Manifest.ID);

        // Harmony.DEBUG = true;
        new Harmony(Manifest.ID).PatchAll();

        ComponentActionMutationManager.RegisterHandler(new InitializationActionHandler(), "SkysCompactCircuits.PackedCircuit");
        WorldHook.worldLoading += () =>
        {
            CustomWS.init();
            //This action is in Unity execution scope, errors must be caught manually:
            try { PackMenu.Build(); }
            catch (Exception e)
            {
                Logger.Error($"Failed to initialize GUI for {Manifest.Name}:");
                SceneAndNetworkManager.TriggerErrorScreen(e);
            }
        };

        PackedCircuitManager.OnIndexAdded += (_, _) => throw new("Indexed data cannot be created directly by the client");

        FuncPacketHandler<IndexCircuitResponsePacket>.Add(packet => PackMenu.AddToHotbar(PackedCircuitManager.Decode(packet.indexCircuitData)));
        FuncPacketHandler<NewCircuitRegisteredPacket>.Add(packet => PackedCircuitManager.RegisterIndex(packet.index, packet.newCircuitData));
        FuncPacketHandler<RemoveIndexedCircuitTrackingPacket>.Add(packet =>
        {
            PackedCircuitManager.CircuitDataByIndex.Remove(packet.indexToRemove);
            foreach (var (_, list) in PackedCircuitManager.IndicesByHash)
                if (list.Remove(packet.indexToRemove))
                    return;
        });
    }

    [Command("CompactCircuits.Server.CullIndexedData")] // Just a shortcut for the console autocomplete mod.
    public static void CullIndexedData() => SceneAndNetworkManager.RunCommandOnServer("CompactCircuits.CullIndexedData");
}
