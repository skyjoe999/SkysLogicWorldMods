
using System;
using LogicAPI.Data;

namespace SkysCompactCircuits.Server;

public struct PackedCircuitStructure
{
    public PartialWorldData AdditionWorld;
    public ushort[] ExportIndices;
    public ComponentAddress[] OriginalChildAddresses;
    public Guid AdditionGuid;
}
