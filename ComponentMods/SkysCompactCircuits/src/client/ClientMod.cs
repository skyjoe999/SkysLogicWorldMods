using System;
using EccsLogicWorldAPI.Client.Hooks;
using FancyInput;
using HarmonyLib;
using LogicAPI.Client;
using LogicWorld;
using LogicWorld.Interfaces;
using LogicWorld.Networking.Handlers;
using LogicWorld.SharedCode.Components;
using SkysCompactCircuits.Client.ClientCode;
using SkysCompactCircuits.Client.Gui;
using SkysCompactCircuits.Client.Keybindings;
using SkysCompactCircuits.Shared;

namespace SkysCompactCircuits.Client;

[HarmonyPatch]
public class SkysCompactCircuits_ClientMod : ClientMod
{
    public const string PackedCircuitTextID = "SkysCompactCircuits.PackedCircuit";
    public const string ExportPegTextID = "SkysCompactCircuits.ExportPeg";
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

        _ = SceneAndNetworkManager.MainWorld; // Runs the static init method (needed for the next line to not error out later)
        WorldInitializationHandler.OnPacketReceived += _ => PackedCircuitManager.ExtraDataManager.SetupExtraData(Instances.MainWorld.ExtraData);
    }
}
