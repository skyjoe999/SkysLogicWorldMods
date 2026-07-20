using HarmonyLib;
using LogicWorld.Server.Circuitry;

namespace SkysCondensedCablingLib.Server;

[HarmonyPatch]
public static class SuperClusterFactory
{
    public static Cluster Create(Cluster cluster, int minSize = 0)
    {
        if (cluster is null || new SuperCluster(cluster, minSize) is not { Size: > 0 } super || (cluster is SuperCluster s && s.Size == super.Size && s.Family == super.Family))
            return cluster;

        cluster.Destroy();
        // We need to re-register that circuit state.
        cluster.CircuitStates.RegisterNewIndex(false);

        foreach (var input in super.ConnectedInputs)
            input.Cluster = super;
        foreach (var input in super.ConnectedInputs)
        {
            foreach (var linkedInput in input.PhasicLinks ?? [])
                if (linkedInput.Cluster is SuperCluster other)
                    super.AddSuperTwoWayLinkWith(other);
                else if (linkedInput.Cluster is not null)
                    super.AddTwoWayLinkWith(linkedInput.Cluster);

            foreach (var followerInput in input.OneWayPhasicLinksFollowers ?? [])
                if (followerInput.Cluster is SuperCluster other)
                    super.AddSuperOneWayLinkTo(other);
                else if (followerInput.Cluster is not null)
                    super.AddOneWayLinkTo(followerInput.Cluster);

            foreach (var leaderInput in input.OneWayPhasicLinksLeaders ?? [])
                if (leaderInput.Cluster is SuperCluster other)
                    other.AddSuperOneWayLinkTo(super);
                else
                    leaderInput.Cluster?.AddOneWayLinkTo(super);


            if (input is not SuperInputPeg sInput)
                continue;

            foreach (var (link, channel) in sInput.PartialPhasicLinks)
                if (link is not SuperInputPeg sOther)
                    super.GetChannel(channel.source).AddPhasicLinkWith(link);
                else if (sOther.SCluster is not null)
                    super.AddTwoWayLinkWith(sOther.SCluster, channel);

            foreach (var (follower, channel) in sInput.PartialOneWayPhasicLinksFollowers)
                if (follower is not SuperInputPeg sOther)
                    super.GetChannel(channel.source).AddOneWayPhasicLinkTo(follower);
                else if (sOther.SCluster is not null)
                    super.AddOneWayLinkTo(sOther.SCluster, channel);

            foreach ((var leader, var channel) in sInput.PartialOneWayPhasicLinksLeaders)
                if (leader is not SuperInputPeg sOther)
                    leader.AddOneWayPhasicLinkTo(super.GetChannel(channel.source));
                else
                    sOther.SCluster?.AddOneWayLinkTo(super, (channel.other, channel.source));
        }
        foreach (OutputPeg output in super.ConnectedOutputs)
            output.ConnectedUpdatables.Add(super);

        // and now the original cluster has been replaced everywhere with the super cluster
        // so we just let the original be deleted
        super.InitializeInnerClusterStatesAfterLinking(); // sets all the outputs
        super.SetOnState(super.States.Any()); // sets the main circuit state
        super.QueueLogicUpdate();

        SuperClusterClientHandler.SendSetup(super);

        return super;
    }

    public static Cluster CreateStarter(Cluster cluster, InputPeg input)
    {
        if (cluster is null || input is not SuperInputPeg { BaseSize: > 0 })
            return cluster;

        var super = new SuperCluster(cluster);
        input.Cluster = super;

        SuperClusterClientHandler.SendSetup(super);

        return super;
    }
}
