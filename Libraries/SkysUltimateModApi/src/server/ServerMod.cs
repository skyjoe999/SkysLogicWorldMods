using LogicAPI.Server;
using SkysUltimateModApi.Shared;

namespace SkysUltimateModApi.Server;

public class SkysUltimateModApi_ServerMod : ServerMod
{
    protected override void Initialize() => UltimateModCompiler.Initialize(Manifest);
}
