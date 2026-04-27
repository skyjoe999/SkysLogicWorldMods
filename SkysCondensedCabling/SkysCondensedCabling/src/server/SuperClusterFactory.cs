using System;
using System.Collections.Generic;
using LICC;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using SkysGeneralLib.Server;
using SkysGeneralLib.Shared.AccessTools;

namespace SkysCondensedCabling.Server;

class SuperClusterFactory
{
    private static readonly IClusterFactory MainFactory = Services.IClusterFactory;
    public static bool IsMainWorld(ClusterFactory factory) => factory == MainFactory;

    public static Cluster Create(Cluster cluster, InputPeg[] inputs, OutputPeg[] outputs)
    {
        LConsole.WriteLine(cluster.AnyLinkedClustersOn());
        foreach (var _input in inputs)
            LConsole.WriteLine(_input.PhasicLinks?.Count ?? -1);
        var super = new SuperCluster(cluster);
        if (super.Size == 0)
            return cluster;
        foreach (var _input in inputs)
        {
            var input = _input.GetSuperPeg() ?? throw new Exception("Non-super input found in super cluster (or the associated component could not be found)");

            foreach (var phasicallyLinkedInput in input.PhasicLinks)
                if (phasicallyLinkedInput.Cluster is SuperCluster other)
                    super.AddSuperTwoWayLinkWith(other);

            foreach (var followerInput in input.OneWayPhasicLinksFollowers)
                if (followerInput.Cluster is SuperCluster other)
                    super.AddSuperOneWayLinkTo(other);

            foreach (var leaderInput in input.OneWayPhasicLinksLeaders)
                if (leaderInput.Cluster is SuperCluster other)
                    other.AddSuperOneWayLinkTo(super);

            foreach ((var phasicallyLinkedInput, var channel) in input.PartialPhasicLinks)
                if (phasicallyLinkedInput.GetCluster() is Cluster other)
                    super.AddTwoWayLinkWith(other, channel);

            foreach ((var followerInput, var channel) in input.PartialOneWayPhasicLinksFollowers)
                if (followerInput.GetCluster() is Cluster other)
                    super.AddOneWayLinkTo(other, channel);

            foreach ((var leaderInput, var channel) in input.PartialOneWayPhasicLinksLeaders)
                if (leaderInput.GetCluster() is Cluster other)
                    super.AddOneWayLinkFrom(other, channel);

            _input.SetCluster(super);
            input.Cluster = super;
        }
        foreach (OutputPeg output in outputs)
            // replace the old one with ours
            ConnectedUpdatables.Get(output)[^1] = super;

        // and now the original cluster has been replaced everywhere with the super cluster
        // so we just let the original be deleted
        // *WITHOUT* calling destroy!
        super.InitializeInnerClusterStatesAfterLinking(); // sets all the outputs
        super.Final.LogicUpdate(); // sets the main circuit state
        super.QueueLogicUpdate();
        return super;
    }

    public static Cluster CreateStarter(Cluster cluster, InputPeg input)
    {
        var super = new SuperCluster(cluster);
        if (super.Size == 0)
            return cluster;
        input.SetCluster(super);
        // probably always fails? But rather be safe than crash the server (in case some other mod is doing something weird)
        // when it does then PegInitializationOverride should fix it
        if (input.GetSuperPeg() is SuperInputPeg peg) peg.Cluster = super;
        return super;
    }
    private static readonly Accessor<OutputPeg, List<ILogicUpdatable>> ConnectedUpdatables = new("ConnectedUpdatables");
}
