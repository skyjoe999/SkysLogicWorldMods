using LogicAPI.Data;
using LogicAPI.Networking.Packets;
using MessagePack;

namespace SkysCompactCircuits.Shared.Packets;

[MessagePackObject]
public sealed class RequestInitializationPacket : Packet
{
    [Key(0)]
    public ComponentAddress componentToInitialize;
}

[MessagePackObject]
public sealed class IndexCircuitRequestPacket : Packet
{
    [Key(0)]
    public byte[] data;
}

[MessagePackObject]
public sealed class IndexCircuitResponsePacket : Packet
{
    [Key(0)]
    public byte[] data;
}
