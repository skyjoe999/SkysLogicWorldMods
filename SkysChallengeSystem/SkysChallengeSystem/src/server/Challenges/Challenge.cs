using System;
using SkysChallengeSystem.Server.Components;
using SkysChallengeSystem.Shared;

namespace SkysChallengeSystem.Server.Challenges;

public abstract class Challenge : IDisposable
{
    // RunningData is the only thing saved on server reset
    private RunningData _RunningData;

    public RunningData RunningData
    {
        get => _RunningData;
        protected set
        {
            if (_RunningData is not null) _RunningData.OnPropertySet -= EmitOnPropertySet;
            _RunningData = value;
            if (_RunningData is not null) _RunningData.OnPropertySet += EmitOnPropertySet;
        }
    }

    protected IChallengeDataAccess ChallengeDataAccess;
    public readonly ChallengeRecord Record;

    protected Challenge(ChallengeRecord record) :
        this(record, new FallbackRunningData())
    {
    }

    protected Challenge(ChallengeRecord record, RunningData runningData)
    {
        Record = record;
        _RunningData = runningData;
        _RunningData.OnPropertySet += EmitOnPropertySet;
    }

    public bool IsRunning => RunningData?.IsRunning ?? false;
    protected abstract void OnBegin();
    protected abstract void OnStep();
    protected abstract bool ShouldWaitForAnswerChange();
    public event Action OnSuccess;
    public event Action OnFailure;
    public event Action OnPropertySet;
    protected virtual bool canBegin() => !IsRunning; // Not sure why you'd override this...

    protected virtual void OnResume()
    {
    }

    protected virtual void OnCancel()
    {
    }

    public virtual void Dispose()
    {
    }

    public bool Begin()
    {
        if (!canBegin()) return false;

        RunningData?.Reset();
        RunningData ??= new FallbackRunningData();
        RunningData.IsRunning = true;
        OnBegin();
        if (!IsRunning) return false; // Safety check
        if (!ShouldWaitForAnswerChange()) ChallengeDataAccess.QueueLogicUpdate();
        return true;
    }

    public void Step()
    {
        if (!IsRunning) return;
        OnStep();
        if (!IsRunning) return; // Might be important if this step was a success or failure
        if (!ShouldWaitForAnswerChange()) ChallengeDataAccess.QueueLogicUpdate();
    }

    protected void Succeed()
    {
        if (!IsRunning) return;
        RunningData.IsRunning = false;
        OnSuccess?.Invoke();
    }

    protected void Fail()
    {
        if (!IsRunning) return;
        RunningData.IsRunning = false;
        OnFailure?.Invoke();
    }

    public void Cancel()
    {
        RunningData.IsRunning = false;
        OnCancel();
    }

    public void Resume(byte[] runningData)
    {
        RunningData.DeserializeData(runningData);
        if (IsRunning) OnResume();
    }

    protected void EmitOnPropertySet() => OnPropertySet?.Invoke();

    public void SetChallengeDataAccess(IChallengeDataAccess obj) => ChallengeDataAccess = obj;

    // only stores running flag
    private class FallbackRunningData : RunningData
    {
        public override bool IsRunning { get; protected internal set; }

        public override void Reset()
        {
        }

        public override byte[] SerializeData() => [(byte)(IsRunning ? 1 : 0)];
        public override void DeserializeData(byte[] data) => IsRunning = data.Length != 0 && data[0] != 0;
    }
}

public abstract class Challenge<TData> : Challenge where TData : class
{
    protected Challenge(ChallengeRecord record) : base(record)
    {
        RunningData = new RunningData<TData>(ResetData, SetDataDefaultValues);
    }

    protected TData Data => ((RunningData<TData>)RunningData).Data;

    protected virtual void ResetData() => SetDataDefaultValues();
    protected abstract void SetDataDefaultValues();
}
