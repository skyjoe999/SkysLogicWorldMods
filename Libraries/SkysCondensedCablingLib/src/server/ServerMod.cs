using System.Linq;
using EccsLogicWorldAPI.Server.Hooks;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Networking;
using LogicAPI.Server;
using LogicWorld.Server.Circuitry;
using SkysGeneralLib.Server;

namespace SkysCondensedCablingLib.Server;

public class SkysCondensedCablingLib_ServerMod : ServerMod, PlayerJoiningHook.PlayerJoiningCallback
{
    public override void Initialize()
    {
        new Harmony(Manifest.ID).PatchAll();
        PlayerJoiningHook.registerCallback(this);
    }

    public void playerIsJoining(Connection connection, PlayerData playerData)
    {
        SuperClusterClientHandler.SetupPlayer(connection,
            Services.ICircuitryManager is CircuitryManager manager
                ? manager.LogicComponents.Values
                : Services.IWorldData.AllComponents.Select(p => Services.ICircuitryManager.LookupComponent(p.Key)).ToList() // slower but technically correct
        );

        // Only needs to run once but Initalize is too early so it goes here.
        SuperClusterClientHandler.RegisterConnectAnyType("MHG.Peg");
        SuperClusterClientHandler.RegisterConnectAnyType("MHG.ThroughPeg");
    }
}

public interface IHasSuperPegs
{
    int InputSuperSize(int index);
    int OutputSuperSize(int index);

    SuperPegFamily InputFamily(int index) => SuperPegFamily.Standard;
    SuperPegFamily OutputFamily(int index) => SuperPegFamily.Standard;
}
