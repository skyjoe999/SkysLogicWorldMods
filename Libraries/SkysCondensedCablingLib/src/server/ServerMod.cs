using System;
using System.Linq;
using EccsLogicWorldAPI.Server.Hooks;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Networking;
using LogicAPI.Server;
using LogicAPI.Server.Components;
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

public static class SuperPegExstentions
{
    public static void EnsureSuperPegsAreCorrect(this IHasSuperPegs component)
    {
        if (component is not LogicComponent lComponent)
            throw new($"{nameof(IHasSuperPegs)} must inherit from {nameof(LogicComponent)}");

        foreach (var input in lComponent.Inputs)
            // Might trigger repeatedly if the cluster is being expanded by another peg. The factory will atleast mitigate this.
            if (input is SuperInputPeg { SCluster: { } sCluster } super && sCluster.Size != super.BaseSize)
                SuperClusterFactory.Create(super.Cluster);

        // Check if pegs need to be premoted or demoted from/to super pegs
        for (int i = 0; i < lComponent.Inputs.Count; i++)
            if ((component.InputSuperSize(i) > 0) != lComponent.Inputs[i] is SuperInputPeg)
            {
                Rebuild();
                return;
            }
        for (int i = 0; i < lComponent.Outputs.Count; i++)
            // Idk if there's an easier way to change the size of output pegs but this should work.
            if ((lComponent.Outputs[i] is SuperOutputPeg super ? super.Size : 0) != Math.Max(component.OutputSuperSize(i), 0))
            {
                Rebuild();
                return;
            }

        void Rebuild()
        {
            // This will delete all pegs so make sure you handle and links properly.
            // WARNING: this relies on CleanModifyPegCounts doing more work than it needs to. This may be changed soon and will break this.
            Services.ICircuitryManager.CleanModifyPegCounts(lComponent.Address, () => { });
            // Should only ever recurse once. 
            // (If it does stack overflow I guess that would also be usefull information...)
            EnsureSuperPegsAreCorrect(component);
        }
    }
}
