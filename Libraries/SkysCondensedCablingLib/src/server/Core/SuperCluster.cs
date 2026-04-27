using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicWorld.Server.Circuitry;

namespace SkysCondensedCablingLib.Server;

[HarmonyPatch]
public class SuperCluster : Cluster
{
    public int Size => States.Size;
    public readonly ClusterStates States;
    public readonly SuperPegFamily Family;


    public bool this[int index] => index < Size && States[index];

    public SuperCluster(Cluster cluster, int minSize = 0) : base(
        cluster.StateID,
        cluster.CircuitStates,
        cluster.Container,
        cluster.ConnectedInputs,
        cluster.ConnectedOutputs,
        cluster.ConnectedUpdatables
    )
    {
        var sizeOut = ConnectedOutputs.Max(input => (input as SuperOutputPeg)?.Size) ?? 0;
        var sizeIn = ConnectedInputs.Max(output => (output as SuperInputPeg)?.BaseSize) ?? 0;
        var sizeLink = cluster.Linker?.LinkedLeaders.Max(link => (link.ClusterBeingLinked as SuperCluster)?.Size) ?? 0;
        var size = Math.Max(Math.Max(minSize, sizeLink), Math.Max(sizeIn, sizeOut));

        States = new(size, this);
        SetOnState(States.Any());

        if (size > 0)
            Family = ConnectedInputs.Select(input => (input as SuperInputPeg)?.BaseFamily)
                .Concat(ConnectedOutputs.Select(output => (output as SuperOutputPeg)?.Family))
                .Concat(cluster.Linker?.LinkedLeaders.Select(link => (link.ClusterBeingLinked as SuperCluster)?.Family) ?? [])
                .FirstOrDefault(f => f is not null);
    }


    public override void LogicUpdate()
    {
        // This will trigger any relevant partial clusters to update just like a normal cluster
        for (int index = 0; index < Size; index++)
            States.Outputs[index].On = ConnectedOutputs.Any(p => p is SuperOutputPeg s ? s[index] : p.On);
        SetOnStateWithLinkerSignaling(States.Outputs.Any(peg => peg.On));
    }

    public override void Destroy()
    {
        foreach (var peg in States.Inputs)
        {
            // These will be re-added by the cluster factory since these pegs are about to be deleted.
            foreach (var link in peg.PhasicLinks?.Duplicate() ?? [])
                peg.RemovePhasicLinkWith(link);
            foreach (var followers in peg.OneWayPhasicLinksFollowers?.Duplicate() ?? [])
                peg.RemoveOneWayPhasicLinkTo(followers);
            foreach (var leaders in peg.OneWayPhasicLinksLeaders?.Duplicate() ?? [])
                leaders.RemoveOneWayPhasicLinkTo(peg);
        }

        foreach (var cluster in States.PartialClusters)
            cluster.Destroy();

        SuperClusterClientHandler.SendCleanup(this);

        base.Destroy();
    }
    public void InitializeInnerClusterStatesAfterLinking()
    {
        for (int index = 0; index < Size; index++)
            States.PartialClusters[index].SetOnState(ConnectedOutputs.Any(p => p is SuperOutputPeg s ? s[index] : p.On) || States.PartialClusters[index].AnyLinkedClustersOn());
    }

    #region Linking
    private IEnumerable<(Cluster, Cluster)> GetPairs(SuperCluster other) => States.PartialClusters.Zip(other.States.PartialClusters, (a, b) => (a, b));
    public void AddSuperTwoWayLinkWith(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.AddTwoWayLinkWith(p.Item2));
    public void RemoveSuperTwoWayLinkWith(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.RemoveTwoWayLinkWith(p.Item2));
    public void AddSuperOneWayLinkTo(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.AddOneWayLinkTo(p.Item2));
    public void RemoveSuperOneWayLinkTo(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.RemoveOneWayLinkTo(p.Item2));

    public InputPeg GetChannel(int channel) => States.Inputs[channel];

    public void AddTwoWayLinkWith(SuperCluster other, (int source, int other) channel)
    {
        if (channel.source < Size && channel.other < other.Size)
            States.PartialClusters[channel.source].AddTwoWayLinkWith(other.States.PartialClusters[channel.other]);
    }

    public void RemoveTwoWayLinkWith(SuperCluster other, (int source, int other) channel)
    {
        if (channel.source < Size && channel.other < other.Size)
            States.PartialClusters[channel.source].RemoveTwoWayLinkWith(other.States.PartialClusters[channel.other]);
    }

    public void AddOneWayLinkTo(SuperCluster other, (int source, int other) channel)
    {
        if (channel.source < Size && channel.other < other.Size)
            States.PartialClusters[channel.source].AddOneWayLinkTo(other.States.PartialClusters[channel.other]);
    }

    public void RemoveOneWayLinkTo(SuperCluster other, (int source, int other) channel)
    {
        if (channel.source < Size && channel.other < other.Size)
            States.PartialClusters[channel.source].RemoveOneWayLinkTo(other.States.PartialClusters[channel.other]);
    }
    #endregion
}
