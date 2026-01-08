namespace SkysChallengeSystem.Shared.ComponentDataDefs;

public interface IChallengePegData
{
    string PegName { get; set; }
}

public interface IChallengeQuestionData : IChallengePegData;

public interface IChallengeAnswerData : IChallengePegData;

public static class IChallengePegDataExtension
{
    public static void SetDataDefaultValues(this IChallengePegData data, bool isOutput)
    {
        data.PegName = null;
    }

    public static void SetDataDefaultValues(this IChallengeQuestionData data)
        => data.SetDataDefaultValues(false);

    public static void SetDataDefaultValues(this IChallengeAnswerData data)
        => data.SetDataDefaultValues(true);
}
