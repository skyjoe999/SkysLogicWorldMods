
using System;
using LogicAPI.Data;

namespace SkysCompactCircuits.Server;

public struct PackedCircuitStructure
{
    public PartialWorldData AdditionWorld;
    public PegAddress[] ExportAddresses;
    public ComponentAddress[] OriginalChildAddresses;
    public Guid AdditionGuid;
    public PartialWorldData UnpackingWorld;
    public Guid UnpackingGuid;
}
