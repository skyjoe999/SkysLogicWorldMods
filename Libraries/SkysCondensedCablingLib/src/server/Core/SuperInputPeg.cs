using System;
using System.Collections.Generic;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;

namespace SkysCondensedCablingLib.Server;

// Can safely wire to and secret link with same type
public class SuperInputPeg(InputPeg input)
    : InputPeg(input.iAddress, input.LogicComponent, input.ShouldTriggerComponentLogicUpdates, input.CircuitryManager)
{
    public SuperCluster SCluster => Cluster as SuperCluster;
    public bool this[int index] => Cluster is SuperCluster sCluster && sCluster[index];
    public int BaseSize => (LogicComponent as IHasSuperPegs)?.InputSuperSize(Address.PegIndex) ?? 0;
    public SuperPegFamily BaseFamily => (LogicComponent as IHasSuperPegs)?.InputFamily(Address.PegIndex);

    #region Phasic (super to generic)
    public readonly List<(IInputPeg other, (int source, int other) channel)> PartialPhasicLinks = [];
    public readonly List<(IInputPeg other, (int source, int other) channel)> PartialOneWayPhasicLinksFollowers = [];
    public readonly List<(IInputPeg other, (int source, int other) channel)> PartialOneWayPhasicLinksLeaders = []; // source still refers to "this"

    public void AddPhasicLinkWith(IInputPeg other, int channel) => AddPhasicLinkWith(other, (channel, -1));
    public void RemovePhasicLinkWith(IInputPeg other, int channel) => RemovePhasicLinkWith(other, (channel, -1));

    public void AddOneWayPhasicLinkTo(IInputPeg other, int channel) => AddOneWayPhasicLinkTo(other, (channel, -1));
    public void RemoveOneWayPhasicLinkTo(IInputPeg other, int channel) => RemoveOneWayPhasicLinkTo(other, (channel, -1));

    public void AddOneWayPhasicLinkFrom(IInputPeg other, int channel) => AddOneWayPhasicLinkFrom(other, (channel, -1));
    public void RemoveOneWayPhasicLinkFrom(IInputPeg other, int channel) => RemoveOneWayPhasicLinkFrom(other, (channel, -1));

    #endregion
    #region Phasic (super to normal)
    public void AddPhasicLinkWith(IInputPeg other, (int source, int other) channel)
    {
        if (other is not SuperInputPeg)
            channel.other = -1;

        if (QueueIfNullCluster(() => AddPhasicLinkWith(other, channel)) || PartialPhasicLinks.Contains((other, channel)))
            return;

        PartialPhasicLinks.Add((other, channel));
        if (other is SuperInputPeg sOther)
        {
            sOther.PartialPhasicLinks.Add((this, (channel.other, channel.source)));
            if (sOther.SCluster is not null)
                SCluster.AddTwoWayLinkWith(sOther.SCluster, channel);
        }
        else
            SCluster.GetChannel(channel.source).AddPhasicLinkWith(other);
    }

    public void RemovePhasicLinkWith(IInputPeg other, (int source, int other) channel)
    {
        if (other is not SuperInputPeg)
            channel.other = -1;

        if (!PartialPhasicLinks.Remove((other, channel)))
            return;

        if (other is SuperInputPeg sOther)
        {
            sOther.PartialPhasicLinks.Remove((this, (channel.other, channel.source)));
            SCluster.RemoveTwoWayLinkWith(sOther.SCluster, channel);
        }
        else
            SCluster.GetChannel(channel.source).RemovePhasicLinkWith(other);
    }


    public void AddOneWayPhasicLinkTo(IInputPeg other, (int source, int other) channel)
    {
        if (other is not SuperInputPeg)
            channel.other = -1;

        if (QueueIfNullCluster(() => AddOneWayPhasicLinkTo(other, channel)) || PartialOneWayPhasicLinksFollowers.Contains((other, channel)))
            return;

        PartialOneWayPhasicLinksFollowers.Add((other, channel));
        if (other is SuperInputPeg sOther)
        {
            sOther.PartialOneWayPhasicLinksLeaders.Add((this, (channel.other, channel.source)));
            if (sOther.SCluster is not null)
                SCluster.AddOneWayLinkTo(sOther.SCluster, channel);
        }
        else
            SCluster.GetChannel(channel.source).AddOneWayPhasicLinkTo(other);
    }

    public void RemoveOneWayPhasicLinkTo(IInputPeg other, (int source, int other) channel)
    {

        if (other is not SuperInputPeg)
            channel.other = -1;

        if (!PartialOneWayPhasicLinksFollowers.Remove((other, channel)))
            return;

        if (other is SuperInputPeg sOther)
        {
            sOther.PartialOneWayPhasicLinksLeaders.Remove((this, (channel.other, channel.source)));
            SCluster.RemoveOneWayLinkTo(sOther.SCluster, channel);
        }
        else
            SCluster.GetChannel(channel.source).RemoveOneWayPhasicLinkTo(other);
    }


    public void AddOneWayPhasicLinkFrom(IInputPeg other, (int source, int other) channel)
    {
        if (other is SuperInputPeg sOther)
        {
            sOther.AddOneWayPhasicLinkTo(this, (channel.other, channel.source));
            return;
        }
        channel.other = -1;

        if (QueueIfNullCluster(() => AddOneWayPhasicLinkFrom(other, channel)) || PartialOneWayPhasicLinksLeaders.Contains((other, channel)))
            return;

        PartialOneWayPhasicLinksLeaders.Add((other, channel));
        other.AddOneWayPhasicLinkTo(SCluster.GetChannel(channel.source));
    }

    public void RemoveOneWayPhasicLinkFrom(IInputPeg other, (int source, int other) channel)
    {
        if (other is SuperInputPeg sOther)
            sOther.RemoveOneWayPhasicLinkTo(this, (channel.other, channel.source));

        else if (PartialOneWayPhasicLinksLeaders.Remove((other, (channel.source, -1))))
            other.RemoveOneWayPhasicLinkTo(SCluster.GetChannel(channel.source));
    }

    private bool QueueIfNullCluster(Action action)
    {
        if (SCluster is not null)
            return false;
        DummyInputManager.QueueLink(action);
        return true;
    }
    #endregion
}
