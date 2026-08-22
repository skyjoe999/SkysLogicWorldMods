using System;
using System.Collections.Generic;
using System.Linq;
using EccsLogicWorldAPI.Server.Hooks;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Networking;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using LogicWorld.Server.Code.Server.Networking.ClientHandlers;
using LogicWorld.SharedCode.Networking;
using SkysCompactCircuits.Shared;
using SkysCompactCircuits.Shared.Packets;
using SkysGeneralLib.Server;
using SkysGeneralLib.Server.TypeExtensions;
using SkysGeneralLib.Shared.AccessTools;
using SkysGeneralLib.Shared.Networking;

namespace SkysCompactCircuits.Server;

[HarmonyPatch]
public class SetupHooks : PlayerJoiningHook.PlayerJoiningCallback
{
    private static event Action BatchInitStarted;

    static SetupHooks()
    {
        FuncPacketHandler<RequestInitializationPacket>.Add(packet => (packet.componentToInitialize.GetLogicComponent() as PackedCircuit)?.EnsureSetupAndSendToClient());
        PlayerJoiningHook.registerCallback(new SetupHooks());
    }

    public static void EnsureSetup(PackedCircuit packedCircuit)
    {
        // Waiting until the first player joins is a great idea to stop redundant setup calls
        // Unfortunately that does mean the circuits will not be initialized in batch mode which will brick the server!!!
        // So yea, we'll just run the code twice...
        BatchInitStarted += packedCircuit.RunSetup;
    }

    [HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.FinalizeBatchClusterInitialization))]
    [HarmonyPrefix] // By doing this before, the game wont try to build all the empty clusters
    public static void BatchFinalizationOverride()
    {
        BatchInitStarted?.Invoke();
        BatchInitStarted = null;
    }

    [HarmonyPatch(typeof(ConnectionEstablishedHandler), nameof(ConnectionEstablishedHandler.Handle))]
    [HarmonyPrefix] // We want to send this before the world initialize packet so all the data is ready before client-codes are spawned.
    public static void PlayerConnectingHook(HandlerContext context)
    {
        // Is sending the data with the hotbar insane? Yes!
        // Is there a better option given that we cannot send custom packets before world init? Not that I can figure out!
        var playerValues = Services.IPlayerSaveManager.GetPlayerValues(context.Sender);
        var allCircuitData = PackedCircuitManager.SerializeData();
        playerValues.Hotbar ??= new() { HotbarItems = [], SelectedHotbarSlot = 0 };
        playerValues.Hotbar.HotbarItems = [.. playerValues.Hotbar.HotbarItems, new DetailedHotbarItemData() { TextID = "SkysCompactCircuits.WorldCircuitInitializationData", CustomData = allCircuitData }];
    }

    public void playerIsJoining(Connection connection, PlayerData playerData)
    {
        // If the Services.ICircuitryManager is custom this mod wont work anyways but...
        // I guess a mod could call BatchFinalizationOverride() if they wanted to remain compatible
        var components = Services.ICircuitryManager is CircuitryManager manager
            ? new Accessor<CircuitryManager, Dictionary<ComponentAddress, LogicComponent>>("LogicComponents").Get(manager).Values
            : Services.IWorldData.AllComponents.Select(p => Services.ICircuitryManager.LookupComponent(p.Key)); // slower but technically correct

        foreach (var component in components)
            if (component is PackedCircuit circuit)
                circuit.EnsureSetupAndSendToClient(); // Will send to all clients but.. I don't care?
    }
}
