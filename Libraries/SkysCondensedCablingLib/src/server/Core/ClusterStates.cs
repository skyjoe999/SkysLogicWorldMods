using System;
using System.Collections.Generic;
using System.Linq;
using LogicWorld.Server;
using LogicWorld.Server.Circuitry;

namespace SkysCondensedCablingLib.Server;

public class ClusterStates : CircuitStates
{
    public readonly int Size;
    public readonly OutputPeg[] Outputs;
    public readonly InputPeg[] Inputs;
    public readonly Cluster[] PartialClusters;
    public ClusterStates(int size, SuperCluster cluster) : base()
    {
        Size = Math.Max(0, size);
        States = new State[Size * 2];
        if (size == 0)
            return;

        // Ensures the pegs have ids [size, size * 2 - 1]
        _UnusedAddresses = new Stack<int>();
        for (int i = 0; i < size; i++)
            _UnusedAddresses.Push(size + size - i - 1);

        Outputs = [.. Enumerable.Range(0, size).Select(i => new OutputPeg(new(default, i), false, this))];
        Inputs = [.. Enumerable.Range(0, size).Select(i => new InputPeg(new(default, i), null, false, null))];

        PartialClusters = new Cluster[size];
        for (int i = 0; i < size; i++)
        {
            var partialCluster = PartialClusters[i] = new Cluster(i, this, cluster.Container, [Inputs[i]], [Outputs[i]], cluster.ConnectedUpdatables);
            Inputs[i].Cluster = partialCluster;
            partialCluster.QueueLogicUpdate();
            Outputs[i].ConnectedUpdatables = [partialCluster];
            partialCluster.Linker = new(partialCluster, Program.Get<SelfUpdatingContainer<ClusterLinker>>());
            partialCluster.AddOneWayLinkTo(cluster);
        }
    }

    public bool Any() => Enumerable.Range(0, Size).Any(i => this[i]);
}
