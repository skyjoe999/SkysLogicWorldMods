using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EccsLogicWorldAPI.Server.Hooks;
using EccsLogicWorldAPI.Shared.AccessHelper;
using HarmonyLib;
using LICC;
using LogicAPI;
using LogicAPI.Data;
using LogicAPI.Networking;
using LogicAPI.Server;
using LogicAPI.WorldDataMutations;
using LogicWorld.Server.Circuitry;
using LogicWorld.Server.Managers;
using LogicWorld.SharedCode;
using SkysColorfulClusters.Shared;
using SkysColorfulClusters.Shared.Packets;
using SkysGeneralLib.Server;
using SkysGeneralLib.Server.TypeExtensions;
using SkysGeneralLib.Shared.Networking;
using SkysGeneralLib.Shared.AccessTools;

namespace SkysColorfulClusters.Server;

public class SkysColorfulClusters_ServerMod : ServerMod
{
    protected override void Initialize()
    {
        var harmony = new Harmony("SkysColorfulClustersServer");
        // Harmony.DEBUG = true;
        harmony.PatchAll();

        FuncPacketHandler<ChangeClusterColorRequest>.Add(packet =>
        {
            ClusterColorUpdateHandler.PauseAutoUpdate();
            foreach ((var pegAddress, var entry) in packet.clusterColors)
                ClusterColorManager.SetClusterColor(pegAddress, entry);
            ClusterColorUpdateHandler.FlushAutoUpdate();
        });


        ClusterColorManager.ClusterColorSet += (stateID, change) => ClusterColorUpdateHandler.PacketJoiner.Queue((stateID, change));
        ClusterColorUpdateHandler.PacketJoiner.OnClear += ClusterColorExtraDataManager.WriteToExtraData;

        // this game is in desperate need of better hooks '^'
        InitCallback.Hook += () => ClusterColorExtraDataManager.SetupExtraData(Services.ExtraData);

    }


    private class InitCallback : PlayerJoiningHook.PlayerJoiningCallback
    {
        public static event Action Hook;
        static InitCallback() => PlayerJoiningHook.registerCallback(new InitCallback());
        public void playerIsJoining(Connection connection, PlayerData playerData) => Hook?.Invoke();
    }

    [Command("ColorfulClusters.ClearAll", Description = "Clears all of the tracked clusters. NO UNDO.")]
    public static void ClearAll()
    {
        ClusterColorManager.ClearAll();
        ClusterColorExtraDataManager.WriteToExtraData();
        Services.NetworkServer.Broadcast(new ForceResetClusterColorsPacket());
    }
}

// This is the mega class, it handles *everything*
// (I prefer to keep all my patches in one file so)
public static class ClusterColorUpdateHandler
{
    #region Packet Handling
    static ClusterColorUpdateHandler()
    {
        PacketJoiner.OnClear += () =>
        {
            ClusterColorExtraDataManager.WriteToExtraData();
            if (OnFlushClearOnce)
                RecentDestroys.Clear();
            OnFlushClearOnce = false;
        };
    }
    public static readonly PacketJoiner<(int, ChangeClusterColorPacket._ClusterColorEntry? color)> PacketJoiner =
        new(data => new ChangeClusterColorPacket { clusterColors = data });

    public static void PauseAutoUpdate() => PacketJoiner.PushPause();
    public static void FlushAutoUpdate() => PacketJoiner.PopPause();
    #endregion


    // All these patches are related to keeping track of and updating cluster colors when clusters are modified
    #region Patches
    #region Helpers
    private static InputPeg LookupInput(InputAddress iAddress) => Services.ICircuitryManager.LookupInput(iAddress);
    private static OutputPeg LookupOutput(OutputAddress oAddress) => Services.ICircuitryManager.LookupOutput(oAddress);
    private static InputPeg LookupInput(PegAddress pAddress) => pAddress.IsInputAddress() ? LookupInput(new InputAddress(pAddress.ComponentAddress, pAddress.PegIndex)) : null;
    private static OutputPeg LookupOutput(PegAddress pAddress) => pAddress.IsOutputAddress() ? LookupOutput(new OutputAddress(pAddress.ComponentAddress, pAddress.PegIndex)) : null;
    private static int LookupStateID(PegAddress pAddress) => pAddress.IsInputAddress() ? ClusterAccess.Get(LookupInput(pAddress)).StateID : LookupOutput(pAddress).StateID;

    private static readonly Accessor<InputPeg, Cluster> ClusterAccess = new("Cluster");
    #endregion

    // If this looks simple, that's because the first dozen attempts sucked!
    // (If it looks complicated... tell me about it! TT)

    // We need to intervene when:
    // - A new cluster is created [ClusterFactoryCreateOverride] (Take the best match from the inputs)
    // - A wire is created [ClusterFactoryCreateOverride] (We need to merge the colors)
    // - "UpdateCircuitModelAtPeg" which is called by:
    //   - A wire is broken [WireSplitter] (Set both ends)
    //   - removing secret links [WireSplitter] (We do our best to set both ends)
    //   - adding secret links [ClusterFactoryCreateOverride]
    //   - "RemoveComponentsAndChildrenAndAttachedWires" [ComponentAndChildrenDestroyOverride] (this is the only way pegs are permanently destroyed)
    //   - changing peg count (as long as no bad data is created, i dont really care what happens)
    // - Cloning [CloneOverride] (we need to match up the clusters)
    // - Subassemblies (Nope!!! Not my problem!!!)

    #region Break+Make clusters
    /// <summary> Keeps track of the colors of destroyed clusters until a new cluster is made (and the packets have been sent). </summary>
    private static readonly Dictionary<PegAddress, ClusterColor> RecentDestroys = [];
    private static bool OnFlushClearOnce = false;

    [HarmonyPatch(typeof(ClusterFactory), nameof(ClusterFactory.Create))]
    private class ClusterFactoryCreateOverride
    {
        private static void Postfix() => PauseAutoUpdate();
        private static void Postfix(ref Cluster __result, InputPeg[] inputs)
        {
            foreach (var input in inputs)
                if (RecentDestroys.TryGetValue(input.Address, out var value))
                {
                    ClusterColorManager.SetClusterColorUnsafe(__result.StateID, new(input.Address, value));
                    break;
                }
            OnFlushClearOnce = true;
            FlushAutoUpdate();
        }
    }

    [HarmonyPatch(typeof(Cluster), nameof(Cluster.Destroy))]
    private static class ClusterDestroyOverride
    {
        private static void Prefix(Cluster __instance)
        {
            if (ClusterColorManager.GetClusterData(__instance.StateID) is ClusterColorEntry entry)
            {
                ClusterColorManager.ClearClusterColor(__instance.StateID);
                RecentDestroys[entry.Address] = entry.Color;
            }
        }
    }
    #endregion

    [HarmonyPatch("ServerWorldDataMutator", "RemoveComponentsAndChildrenAndAttachedWires")]
    private static class ComponentAndChildrenDestroyOverride
    {
        private static ComponentAddress[] Roots;
        private static void Prefix(WorldMutation_RemoveComponentsAndChildrenAndAttachedWires mutation)
        {
            PauseAutoUpdate();
            Roots = mutation.AddressesOfComponentsToRemove;
        }

        private static void Postfix()
        {
            FlushAutoUpdate();
            Roots = null;
        }

        [HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.RemoveComponentFromCircuitModel))]
        private static class ComponentDestroyOverride
        {
            private static void Prefix(ComponentAddress cAddress)
            {
                foreach (OutputAddress oAddress in Services.IWorldData.GetOutputAddressesOn(cAddress))
                {
                    var output = Services.ICircuitryManager.LookupOutput(oAddress);
                    ClusterColorManager.ClearClusterColor(output.StateID);
                }
                if (Roots is null) return;
                foreach (InputAddress iAddress in Services.IWorldData.GetInputAddressesOn(cAddress))
                {
                    var input = LookupInput(iAddress);
                    var cluster = ClusterAccess.Get(input);
                    if (cluster is null || ClusterColorManager.GetPrimaryPeg(cluster.StateID) != input.Address)
                        continue;
                    // Not the fastest but should only run once per colored cluster affected
                    // These new colors will get cleared in a second but will be set again right after
                    if (cluster.ConnectedInputs.FirstOrDefault(other => isSafe(other.Address.ComponentAddress)) is InputPeg other)
                        ClusterColorManager.SetClusterColorUnsafe(cluster.StateID, other.Address, ClusterColorManager.GetClusterColor(cluster.StateID));
                }
                bool isSafe(ComponentAddress address) => !Roots.Any(r => address == r || address.DescendsFrom(r));
            }
        }
    }
    private static class WireSplitter
    {
        private static ClusterColorEntry? Data;
        private static (PegAddress, PegAddress) Points;

        [HarmonyPatch("ServerWorldDataMutator", "RemoveWire")]
        private static class RemoveWireOverride
        {
            private static void Prefix(WorldMutation_RemoveWire mutation)
            {
                var wire = Services.IWorldData.Lookup(mutation.AddressOfWireToRemove);
                Data = wire.Point1.IsOutputAddress() || wire.Point2.IsOutputAddress() ? null
                    : ClusterColorManager.GetClusterData(wire.StateID);
                if (Data is null) return;
                PauseAutoUpdate();
                Points = (wire.Point1, wire.Point2);
            }

            private static void Postfix() => PostfixPegs();

        }

        [HarmonyPatch(typeof(InputPeg), nameof(InputPeg.RemoveSecretLinkWith), [typeof(InputPeg)])]
        private static class RemoveSecretLinkOverride
        {
            private static void Prefix(InputPeg __instance, InputPeg other)
            {
                Points = (__instance.Address, other.Address);
                // best we can do, if the cluster is gone it's gone... (hopefully something else picks up the slack)
                Data = ClusterColorManager.GetClusterData(ClusterAccess.Get(__instance)?.StateID ?? -1);
                if (Data is null) return;
                PauseAutoUpdate();
            }

            private static void Postfix() => PostfixPegs();
        }

        private static void PostfixPegs()
        {
            if (Data is ClusterColorEntry data)
            {
                var otherID = LookupStateID(Points.Item1);
                var primaryID = LookupStateID(data.Address);
                if (otherID == primaryID)
                    otherID = LookupStateID(Points.Item2);

                if (otherID != primaryID) // the other cluster is not yet colored (not in a loop)
                    ClusterColorManager.SetClusterColorUnsafe(otherID, Points.Item1, ClusterColorManager.GetClusterColor(primaryID));
                FlushAutoUpdate();
            }
        }
    }

    // This doesn't even work properly in the base game half the time!
    // (This comment was made before testing... this actually works?)
    [HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.CleanModifyPegCounts))]
    private static class CleanModifyPegCountsOverride
    {
        private static void Prefix() => PauseAutoUpdate();
        private static void Postfix() => FlushAutoUpdate();
    }

    #region Cloning
    [HarmonyPatch("ServerWorldDataMutator", "CloneComponents")]
    private static class CloneOverride
    {
        static IReadOnlyDictionary<ComponentAddress, ComponentAddress> OldToNew;
        private static void Postfix()
        {
            if (OldToNew is null)
                return;
            PauseAutoUpdate();
            // We only want to set each cluster once so we need to collect the data first
            Dictionary<int, (InputAddress target, ClusterColorEntry entry)?> clusterData = [];
            foreach ((var old, var clone) in OldToNew)
            {
                foreach (InputAddress iAddress in Services.IWorldData.GetInputAddressesOn(old))
                {
                    var stateID = ClusterAccess.Get(LookupInput(iAddress)).StateID;
                    if (!clusterData.TryGetValue(stateID, out var existing))
                        clusterData[stateID] = (
                            ClusterColorManager.GetClusterData(stateID) is ClusterColorEntry d ? (new(clone, iAddress.PegIndex), d) : null
                        );
                    else if (existing is (_, var entry) && entry.Address == iAddress) // try to match the primary peg when possible
                        clusterData[stateID] = (new(clone, iAddress.PegIndex), entry);
                }
                // Outputs can just be handled quickly because they dont have clusters
                foreach (OutputAddress oAddress in Services.IWorldData.GetOutputAddressesOn(old))
                    if (ClusterColorManager.GetClusterColor(LookupOutput(oAddress).StateID) is ClusterColor color)
                        ClusterColorManager.SetClusterColorUnsafe(LookupOutput(new OutputAddress(clone, oAddress.PegIndex)).StateID, new OutputAddress(clone, oAddress.PegIndex), color);
            }
            foreach ((_, var data) in clusterData)
                if (data is (var target, var entry))
                    ClusterColorManager.SetClusterColorUnsafe(ClusterAccess.Get(LookupInput(target)).StateID, target, entry.Color);
            FlushAutoUpdate();
            OldToNew = null;
        }
        [HarmonyPatch]
        private static class CloneHelperOverride
        {

            static MethodInfo TargetMethod()
            {
                return Types.findInAssembly(typeof(Colors), "LogicWorld.SharedCode.WorldDataMutationHelpers")
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).First(m => m.Name == "CloneComponents" && m.GetParameters().Length > 2);
            }

            private static void Postfix(ref IReadOnlyDictionary<ComponentAddress, ComponentAddress> oldComponentAddressToNew) => OldToNew = oldComponentAddressToNew;
        }
    }
    #endregion

    // Just to be extra sure... (everything was tested without this first so other mods shouldn't break it but it may still cut down on packets)
    [HarmonyPatch(typeof(WorldMutationManager), nameof(WorldMutationManager.ApplyMutationLocallyAndQueueToSendUpdateToClients))]
    private static class MutationOverride
    {
        private static void Prefix() => PauseAutoUpdate();
        private static void Postfix() => FlushAutoUpdate();
    }
    #endregion
}
