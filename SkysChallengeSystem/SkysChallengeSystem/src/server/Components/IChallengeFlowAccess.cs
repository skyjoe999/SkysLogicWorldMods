using SkysChallengeSystem.Server.Challenges;

namespace SkysChallengeSystem.Server.Components;

public interface IChallengeFlowAccess
{
    void Begin();
    void Cancel();
    void Step();

    void SetChallenge(Challenge challenge);

    void SetChallenge(string fullPath)
        => SetChallenge(ChallengeManager.LoadChallenge(fullPath));
}