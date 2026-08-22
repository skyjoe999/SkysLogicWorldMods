using LogicAPI.Data;
using LogicAPI.Networking.Packets;
using MessagePack;

namespace SkysCompactCircuits.Shared.Packets;

[MessagePackObject] // Sent by the client when a compact circuit needs its addon information re-sent.
public sealed class RequestInitializationPacket : Packet
{
    [Key(0)] public ComponentAddress componentToInitialize;
}

[MessagePackObject] // Sent by the client when it wants to request a new circuit be indexed.
public sealed class IndexCircuitRequestPacket : Packet
{
    [Key(0)] public byte[] rawCircuitData;
}

[MessagePackObject] // Responds to a IndexCircuitRequestPacket with the index data after the corresponding NewCircuitRegisteredPacket.
public sealed class IndexCircuitResponsePacket : Packet
{
    [Key(0)] public byte[] indexCircuitData;
}

[MessagePackObject] // Sent to the client when a new circuit is added.
public sealed class NewCircuitRegisteredPacket : Packet
{
    [Key(0)] public int index;
    [Key(1)] public byte[] newCircuitData;
}

[MessagePackObject] // Sent when manually culling the registry.
public sealed class RemoveIndexedCircuitTrackingPacket : Packet
{
    [Key(0)] public int indexToRemove;
}
