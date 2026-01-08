using System.Linq;
using JECS;
using JimmysUnityUtilities;
using SkysChallengeSystem.Server.Challenges;
using SkysChallengeSystem.Shared;

namespace SkysChallengeSystem.Server.Loaders;

public class BasicChallengeLoader : ChallengeLoader<BasicChallengeLoader.BasicChallengeRecord>
{
    public record BasicChallengeRecord : ChallengeRecord
    {
        [SaveThis] public BasicChallengeStep[] Steps;

        // When true a step with Error = null will use the previous error instead of Display
        [SaveThis] public bool TrackError = false;

        // The message to display once all steps have been passed (leave null for no change)
        [SaveThis] public string FinalDisplay = null;

        public record BasicChallengeStep
        {
            // What answer indices changing will cause this step to run (leave null for all)
            [SaveThis] public int[] TriggerPins = null;

            // If true any of the trigger pins going from high to low will trigger this step
            [SaveThis] public bool TriggerOnFalling = false;

            // If true this step will be triggered even when a trigger peg is on even if it was already on
            // (Should not really be combined with TriggerOnFalling as it will then always trigger after one tick)
            [SaveThis] public bool AllowImmediateTrigger = false;

            // If true the challenge will fail if the answer pins do not match the valid answer when this step is triggered
            [SaveThis] public bool FailOnInvalid = false;

            // If true this step will be triggered one logic tick after the previous one
            [SaveThis] public bool DontWaitForTrigger = false;

            // A value of -1 means ignore while 0 or 1 means require high or low respectively
            // Leave null for guaranteed success
            [SaveThis] public int[] ValidAnswers = null;

            // The signal that will be provider to the user on this step
            // Leave null for no change (or all off if this is the first step)
            // If the array is shorter than the number of question pegs, the rest will remain unchanged
            [SaveThis] public bool[] QuestionValues = null;

            // A message to set on the displays (leave null for no change)
            [SaveThis] public string Display = null;

            // The error message to display if this step fails
            // Never used if ValidAnswers is null or FailOnInvalid is false
            // (leave null for same as Display or same as previous if TrackError is true)
            [SaveThis] public string Error = null;
        }
    }

    public override Challenge LoadChallenge(BasicChallengeRecord record) => new BasicChallenge(record);

    public class BasicChallenge : Challenge<BasicChallenge.BasicChallengeData>
    {
        protected new BasicChallengeRecord Record => (BasicChallengeRecord)base.Record;
        public BasicChallengeRecord.BasicChallengeStep CurrentStep => Record.Steps[Data.CurrentStepIndex];

        private bool[] CurrentAnswers => ChallengeDataAccess.GetAnswers(Record.AnswerCount);
        private string PreviousDisplay;
        private string PreviousError;

        public BasicChallenge(BasicChallengeRecord record) : base(record)
        {
            OnSuccess += () => ChallengeDataAccess.ClearQuestions(Record.QuestionCount);
        }

        private bool[] PreviousAnswers
        {
            get => Data.PreviousAnswers.Convert(b => b != 0);
            set => Data.PreviousAnswers = value.Convert(b => b ? (byte)1 : (byte)0);
        }

        protected override void OnBegin()
        {
            ChallengeDataAccess.ClearDisplays();
            // I don't think it makes sense to reference the starting state (right?) so we're defaulting to 0
            // PreviousAnswers = CurrentAnswers;
            PreviousDisplay = "";
            PreviousError = Record.TrackError ? "" : null;
            SetupStep();
        }

        protected override void OnStep()
        {
            if (!CurrentStep.DontWaitForTrigger)
            {
                // Check for triggers
                if (!(CurrentStep.TriggerPins ?? Enumerable.Range(0, Record.AnswerCount))
                    .Any(i =>
                        (PreviousAnswers[i] != ChallengeDataAccess.GetAnswer(i) ||
                         CurrentStep.AllowImmediateTrigger) &&
                        (!PreviousAnswers[i] || CurrentStep.TriggerOnFalling)
                    ))
                {
                    PreviousAnswers = CurrentAnswers;
                    return;
                }
            }

            OnTrigger();
        }

        protected void OnTrigger()
        {
            PreviousAnswers = CurrentAnswers;
            if (CurrentStep.ValidAnswers is not null &&
                CurrentStep.ValidAnswers.Where((answer, i) =>
                    answer != -1 &&
                    answer != 0 != PreviousAnswers[i]
                ).Any())
            {
                if (!CurrentStep.FailOnInvalid) return;
                ChallengeDataAccess.SetErrorMessage(CurrentStep.Error ?? PreviousError ?? PreviousDisplay);
                Fail();
                return;
            }

            if (++Data.CurrentStepIndex >= Record.Steps.Length)
            {
                // Success!!!
                if (Record.FinalDisplay is not null) ChallengeDataAccess.SetDisplayMessage(Record.FinalDisplay);
                Succeed();
                return;
            }

            SetupStep();
        }

        protected void SetupStep()
        {
            // Setup Question Values
            if (CurrentStep.QuestionValues is not null)
                for (var i = 0; i < CurrentStep.QuestionValues.Length; i++)
                    ChallengeDataAccess.SetQuestion(i, CurrentStep.QuestionValues[i]);

            // Setup Display
            if (CurrentStep.Display is null) return;
            ChallengeDataAccess.SetDisplayMessage(PreviousDisplay = CurrentStep.Display);

            // Setup Error
            if (!Record.TrackError) PreviousError = PreviousDisplay;
        }

        protected override bool ShouldWaitForAnswerChange() => !CurrentStep.DontWaitForTrigger;

        protected override void OnResume()
        {
            JustFixItPlease(); // Should do nothing

            // Setup the question values correctly
            var PreviousQuestions = new bool[Record.QuestionCount];
            foreach (var step in Record.Steps[..Data.CurrentStepIndex])
                if (step.QuestionValues is not null)
                    for (var j = 0; j < step.QuestionValues.Length; j++)
                        PreviousQuestions[j] = step.QuestionValues[j];
            for (var i = 0; i < PreviousQuestions.Length; i++)
                ChallengeDataAccess.SetQuestion(i, PreviousQuestions[i]);

            // Setup displays correctly
            PreviousDisplay = "";
            foreach (var step in Record.Steps[..Data.CurrentStepIndex])
                if (step.Display is not null)
                    PreviousDisplay = step.Display;
            ChallengeDataAccess.SetDisplayMessage(PreviousDisplay);

            // Setup error correctly
            if (!Record.TrackError) return;
            PreviousError = "";
            foreach (var step in Record.Steps[..Data.CurrentStepIndex])
                if (step.Error is not null)
                    PreviousError = step.Error;
        }

        private void JustFixItPlease()
        {
            // If this ever runs something bad happened
            // but sometimes things happen and the alternative is never being able to open the world again so..
            if (
                Data.CurrentStepIndex >= Record.Steps.Length ||
                Data.PreviousAnswers is null ||
                Data.PreviousAnswers.Length != Record.AnswerCount
            ) SetDataDefaultValues();
        }

        protected override void OnCancel()
        {
            ChallengeDataAccess.ClearQuestions(Record.QuestionCount);
        }

        public interface BasicChallengeData
        {
            int CurrentStepIndex { get; set; }
            byte[] PreviousAnswers { get; set; }
        }

        // Called on reset too by default
        protected override void SetDataDefaultValues()
        {
            Data.CurrentStepIndex = 0;
            Data.PreviousAnswers = new byte[Record.AnswerCount];
        }
    }
}
