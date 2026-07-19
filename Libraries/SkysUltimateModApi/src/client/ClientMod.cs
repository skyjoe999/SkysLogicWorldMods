using LogicAPI.Client;
using SkysUltimateModApi.Shared;

namespace SkysUltimateModApi.Client;

public class SkysUltimateModApi_ClientMod : ClientMod
{
    protected override void Initialize() => UltimateModCompiler.Initialize(Manifest);
}
