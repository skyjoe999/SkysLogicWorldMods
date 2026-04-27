using System.Collections.Generic;
using JimmysUnityUtilities;
using LogicAPI.Networking.Packets;
using MessagePack;

namespace SkysCondensedCablingLib.Shared;

[MessagePackObject]
public sealed class UpdateSuperClusterPacket : Packet
{
    [Key(0)] public int StateID;
    [Key(1)] public (Color24 off, Color24 on)? Color;
    [Key(2)] public int? ConnectionID;
}

[MessagePackObject]
public sealed class BulkSuperClusterPacket : Packet
{
    [Key(0)] public List<(int StateID, (Color24 off, Color24 on)? Color, int? ConnectionID)> values;
}
