using System;
using System.Collections.Generic;
using System.Linq;
using EccsLogicWorldAPI.Server.Hooks;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Networking;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using SkysCompactCircuits.Shared.Packets;
using SkysGeneralLib.Server;
using SkysGeneralLib.Server.TypeExtensions;
using SkysGeneralLib.Shared.AccessTools;
using SkysGeneralLib.Shared.Networking;

namespace SkysCompactCircuits.Server;

[HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.FinalizeBatchClusterInitialization))]
public class SetupHooks : PlayerJoiningHook.PlayerJoiningCallback
{
    public static bool PlayerHasJoined = false;
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
        // if (PlayerHasJoined)
        BatchInitStarted += packedCircuit.RunSetup;
    }

    private static event Action BatchInitStarted; // By doing this before, the game wont try to build all the empty clusters
    public static void Prefix()
    {
        BatchInitStarted?.Invoke();
        BatchInitStarted = null;
    }

    public void playerIsJoining(Connection connection, PlayerData playerData)
    {
        // If the Services.ICircuitryManager is custom this mod wont work anyways but...
        // I guess a mod could call Prefix() if they wanted to remain compatible
        var components = Services.ICircuitryManager is CircuitryManager manager
            ? new Accessor<CircuitryManager, Dictionary<ComponentAddress, LogicComponent>>("LogicComponents").Get(manager).Values
            : Services.IWorldData.AllComponents.Select(p => Services.ICircuitryManager.LookupComponent(p.Key)); // slower but technically correct

        PlayerHasJoined = true;
        foreach (var component in components)
            if (component is PackedCircuit circuit)
                circuit.EnsureSetupAndSendToClient(); // Will send to all clients but.. I don't care?
    }
}
