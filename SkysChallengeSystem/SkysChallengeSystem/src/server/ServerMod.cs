using LogicAPI.Server;

namespace SkysChallengeSystem.Server;

public class SkysChallengeSystem_ServerMod : ServerMod
{
    protected override void Initialize()
    {
        ChallengeManager.Logger = LoggerFactory.CreateLogger("Server"+nameof(ChallengeManager));
    }
}
