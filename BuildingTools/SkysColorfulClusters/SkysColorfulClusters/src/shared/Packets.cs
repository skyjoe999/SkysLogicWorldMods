using System.Collections.Generic;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Networking.Packets;
using MessagePack;

namespace SkysColorfulClusters.Shared.Packets;

[MessagePackObject]
public sealed class ChangeClusterColorPacket : Packet
{
    [Key(0)]
    public List<(int stateID, _ClusterColorEntry? value)> clusterColors;

    [MessagePackObject] // Message pack does not like readonly structs!
    public struct _ClusterColorEntry(PegAddress address, Color24 off, Color24 on)
    {
        [Key(0)] public PegAddress Address = address;
        [Key(1)] public Color24 Off = off;
        [Key(2)] public Color24 On = on;
        public static implicit operator _ClusterColorEntry(ClusterColorEntry value) => new(value.Address, value.Off, value.On);
        public static implicit operator ClusterColorEntry(_ClusterColorEntry value) => new(value.Address, value.Off, value.On);
    }
}

[MessagePackObject]
public sealed class ChangeClusterColorRequest : Packet
{
    [Key(0)]
    public List<(PegAddress address, _ClusterColor? value)> clusterColors;

    [MessagePackObject] // Message pack does not like readonly structs!
    public struct _ClusterColor(Color24 off, Color24 on)
    {
        [Key(0)] public Color24 Off = off;
        [Key(1)] public Color24 On = on;
        public static implicit operator _ClusterColor(ClusterColor value) => new(value.Off, value.On);
        public static implicit operator ClusterColor(_ClusterColor value) => new(value.Off, value.On);
    }
}

// An empty packet... this is what I have been reduced to!
// (according to ecconia an actually empty packet can crash on windows... *shrug*)
[MessagePackObject]
public sealed class ForceResetClusterColorsPacket : Packet
{
    [Key(0)] public bool CrashResistantDummyBool;
}
