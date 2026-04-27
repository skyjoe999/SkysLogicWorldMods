using System.Collections.Generic;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using SkysGeneralLib.Shared.AccessTools;

namespace SkysCondensedCabling.Server;

public partial class SuperInputPeg
{
    #region Phasic (super to super)
    public readonly List<SuperInputPeg> PhasicLinks = [];
    public readonly List<SuperInputPeg> OneWayPhasicLinksFollowers = [];
    public readonly List<SuperInputPeg> OneWayPhasicLinksLeaders = [];


    public void AddPhasicLinkWith(IInputPeg other) => AddPhasicLinkWith((SuperInputPeg)other);
    public void AddPhasicLinkWithUnsafe(IInputPeg other) => AddPhasicLinkWithUnsafe((SuperInputPeg)other);
    public void RemovePhasicLinkWith(IInputPeg other) => RemovePhasicLinkWith((SuperInputPeg)other);
    public void RemovePhasicLinkWithUnsafe(IInputPeg other) => RemovePhasicLinkWithUnsafe((SuperInputPeg)other);
    public void AddOneWayPhasicLinkTo(IInputPeg other) => AddOneWayPhasicLinkTo((SuperInputPeg)other);
    public void RemoveOneWayPhasicLinkTo(IInputPeg other) => RemoveOneWayPhasicLinkTo((SuperInputPeg)other);

    public void AddPhasicLinkWith(SuperInputPeg other)
    {
        if (!PhasicLinks.Contains(other))
            AddPhasicLinkWithUnsafe(other);
    }


    public void AddPhasicLinkWithUnsafe(SuperInputPeg other)
    {
        PhasicLinks.Add(other);
        other.PhasicLinks.Add(this);
        if (Cluster != null && other.Cluster != null)
            Cluster.AddSuperTwoWayLinkWith(other.Cluster);
    }


    public void RemovePhasicLinkWith(SuperInputPeg other)
    {
        if (PhasicLinks.Contains(other))
            RemovePhasicLinkWithUnsafe(other);
    }


    public void RemovePhasicLinkWithUnsafe(SuperInputPeg other)
    {
        PhasicLinks.Remove(other);
        other.PhasicLinks.Remove(this);
        Cluster.RemoveSuperTwoWayLinkWith(other.Cluster);
    }


    public void AddOneWayPhasicLinkTo(SuperInputPeg other)
    {
        if (!OneWayPhasicLinksFollowers.Contains(other))
        {
            OneWayPhasicLinksFollowers.Add(other);
            other.OneWayPhasicLinksLeaders.Add(this);
            if (Cluster != null && other.Cluster != null)
                Cluster.AddSuperOneWayLinkTo(other.Cluster);
        }
    }

    public void RemoveOneWayPhasicLinkTo(SuperInputPeg other)
    {
        if (OneWayPhasicLinksFollowers.Contains(other))
        {
            OneWayPhasicLinksFollowers.Remove(other);
            other.OneWayPhasicLinksLeaders.Remove(this);
            Cluster.RemoveSuperOneWayLinkTo(other.Cluster);
        }
    }
    #endregion

    #region Phasic (super to normal)
    public readonly List<(InputPeg, int)> PartialPhasicLinks = [];
    public readonly List<(InputPeg, int)> PartialOneWayPhasicLinksFollowers = [];
    public readonly List<(InputPeg, int)> PartialOneWayPhasicLinksLeaders = [];

    public void AddPhasicLinkWith(IInputPeg other, int channel) => AddPhasicLinkWith((InputPeg)other, channel);
    public void AddPhasicLinkWithUnsafe(IInputPeg other, int channel) => AddPhasicLinkWithUnsafe((InputPeg)other, channel);
    public void RemovePhasicLinkWith(IInputPeg other, int channel) => RemovePhasicLinkWith((InputPeg)other, channel);
    public void RemovePhasicLinkWithUnsafe(IInputPeg other, int channel) => RemovePhasicLinkWithUnsafe((InputPeg)other, channel);
    public void AddOneWayPhasicLinkTo(IInputPeg other, int channel) => AddOneWayPhasicLinkTo((InputPeg)other, channel);
    public void RemoveOneWayPhasicLinkTo(IInputPeg other, int channel) => RemoveOneWayPhasicLinkTo((InputPeg)other, channel);
    public void AddOneWayPhasicLinkFrom(IInputPeg other, int channel) => AddOneWayPhasicLinkFrom((InputPeg)other, channel);
    public void RemoveOneWayPhasicLinkFrom(IInputPeg other, int channel) => RemoveOneWayPhasicLinkFrom((InputPeg)other, channel);

    public void AddPhasicLinkWith(InputPeg other, int channel)
    {
        if (!PartialPhasicLinks.Contains((other, channel)))
            AddPhasicLinkWithUnsafe(other, channel);
    }

    public void AddPhasicLinkWithUnsafe(InputPeg other, int channel)
    {
        if (Cluster is null)
        {
            DummyInputManager.MakeDummyTwoWay(this, channel, other);
            return;
        }
        PartialPhasicLinks.Add((other, channel));
        var otherLinks = _PhasicLinksAccess.Get(other);
        if (otherLinks is null)
            _PhasicLinksAccess.Set(other, otherLinks = []);
        otherLinks.Add(Cluster?.GetChannel(channel));

        if (Cluster != null && other.GetCluster() is Cluster cluster)
            Cluster.AddTwoWayLinkWith(cluster, channel);
    }

    public void RemovePhasicLinkWith(InputPeg other, int channel)
    {
        if (PartialPhasicLinks.Contains((other, channel)))
            RemovePhasicLinkWithUnsafe(other, channel);
    }

    public void RemovePhasicLinkWithUnsafe(InputPeg other, int channel)
    {
        PartialPhasicLinks.Remove((other, channel));
        _PhasicLinksAccess.Get(other).Remove(Cluster.GetChannel(channel));
        Cluster.RemoveTwoWayLinkWith(other.GetCluster(), channel);
    }

    public void AddOneWayPhasicLinkTo(InputPeg other, int channel)
    {
        if (!PartialOneWayPhasicLinksFollowers.Contains((other, channel)))
        {
            if (Cluster is null)
            {
                DummyInputManager.MakeDummyTo(this, channel, other);
                return;
            }
            var otherLinks = _OneWayPhasicLinksLeadersAccess.Get(other);
            if (otherLinks is null)
                _OneWayPhasicLinksLeadersAccess.Set(other, otherLinks = []);
            otherLinks.Add(Cluster.GetChannel(channel));

            PartialOneWayPhasicLinksFollowers.Add((other, channel));
            if (Cluster != null && other.GetCluster() is Cluster cluster)
                Cluster.AddOneWayLinkTo(cluster, channel);
        }
    }

    public void RemoveOneWayPhasicLinkTo(InputPeg other, int channel)
    {
        if (PartialOneWayPhasicLinksFollowers.Contains((other, channel)))
        {
            _OneWayPhasicLinksLeadersAccess.Get(other).Remove(Cluster.GetChannel(channel));
            PartialOneWayPhasicLinksFollowers.Remove((other, channel));
            Cluster.RemoveOneWayLinkTo(other.GetCluster(), channel);
        }
    }

    public void AddOneWayPhasicLinkFrom(InputPeg other, int channel)
    {
        if (!PartialOneWayPhasicLinksLeaders.Contains((other, channel)))
        {
            if (Cluster is null)
            {
                DummyInputManager.MakeDummyFrom(this, channel, other);
                return;
            }
            var otherLinks = _OneWayPhasicLinksFollowersAccess.Get(other);
            if (otherLinks is null)
                _OneWayPhasicLinksFollowersAccess.Set(other, otherLinks = []);
            otherLinks.Add(Cluster.GetChannel(channel));

            PartialOneWayPhasicLinksLeaders.Add((other, channel));
            if (Cluster != null && other.GetCluster() is Cluster cluster)
                Cluster.AddOneWayLinkFrom(cluster, channel);
        }
    }

    public void RemoveOneWayPhasicLinkFrom(InputPeg other, int channel)
    {
        if (PartialOneWayPhasicLinksLeaders.Contains((other, channel)))
        {
            _OneWayPhasicLinksFollowersAccess.Get(other).Remove(Cluster.GetChannel(channel));
            PartialOneWayPhasicLinksLeaders.Remove((other, channel));
            Cluster.RemoveOneWayLinkFrom(other.GetCluster(), channel);
        }
    }
    #endregion
    private static readonly Accessor<InputPeg, List<InputPeg>> _PhasicLinksAccess = new("_PhasicLinks");
    private static readonly Accessor<InputPeg, List<InputPeg>> _OneWayPhasicLinksFollowersAccess = new("_OneWayPhasicLinksFollowers");
    private static readonly Accessor<InputPeg, List<InputPeg>> _OneWayPhasicLinksLeadersAccess = new("_OneWayPhasicLinksLeaders");
}