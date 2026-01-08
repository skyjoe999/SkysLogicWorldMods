using LogicAPI.Server;
using SkysChallengeSystem.Server;

namespace ExampleChallenges.Server;

public class ExampleChallenges_ServerMod : ServerMod
{
    protected override void Initialize()
    {
        ChallengeManager.RegisterChallenges(Files, Manifest);
    }
}