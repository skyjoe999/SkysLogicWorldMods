using JECS;
using LICC;
using SkysChallengeSystem.Server.Challenges;
using SkysChallengeSystem.Shared;

namespace SkysChallengeSystem.Server.Loaders;

public class DebugChallengeLoader : ChallengeLoader<DebugChallengeLoader.DebugChallengeRecord>
{
    public record DebugChallengeRecord : ChallengeRecord
    {
        [SaveThis] public string Print = "test";
    }

    public override Challenge LoadChallenge(DebugChallengeRecord record)
    {
        LConsole.WriteLine("Print: " + record.Print);
        return new DebugChallenge(record);
    }

    private class DebugChallenge : Challenge
    {
        public DebugChallenge(DebugChallengeRecord record) : base(record)
        {
            OnSuccess += () => LConsole.WriteLine("OnSuccess: " + Record.Name);
            OnFailure += () => LConsole.WriteLine("OnFailure: " + Record.Name);
        }

        protected override void OnBegin() => LConsole.WriteLine("OnBegin: " + Record.Name);
        protected override void OnStep() => LConsole.WriteLine("OnStep: " + Record.Name);

        protected override bool ShouldWaitForAnswerChange()
        {
            LConsole.WriteLine("ShouldWaitForAnswerChange: " + Record.Name);
            return true;
        }

        protected override void OnCancel() => LConsole.WriteLine("OnCancel: " + Record.Name);
        protected override void OnResume() => LConsole.WriteLine("OnResume: " + Record.Name);

        public override void Dispose() => LConsole.WriteLine("Dispose: " + Record.Name);
    }
}