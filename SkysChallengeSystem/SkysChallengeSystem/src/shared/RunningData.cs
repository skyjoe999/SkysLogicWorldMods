using System;
using LogicAPI.Interfaces;
using LogicWorld.SharedCode.BinaryStuff;

namespace SkysChallengeSystem.Shared;

public abstract class RunningData
{
    public abstract bool IsRunning { get; protected internal set; }

    public abstract void Reset();

    public virtual byte[] SerializeData() => null;

    public virtual void DeserializeData(byte[] data)
    {
    }

    public event Action OnPropertySet;
    protected void EmitOnPropertySet() => OnPropertySet?.Invoke();
}

public class RunningData<TData> : RunningData where TData : class
{
    private readonly Action OnReset;
    private readonly Action SetDefault;
    protected readonly ICustomDataManager<TData> DataManager;
    public TData Data => DataManager.Data;

    private bool _isRunning;

    public override bool IsRunning
    {
        get => _isRunning;
        protected internal set
        {
            _isRunning = value;
            EmitOnPropertySet();
        }
    }

    public RunningData(Action OnReset, Action SetDefault)
    {
        this.OnReset = OnReset;
        this.SetDefault = SetDefault;
        DataManager = new CustomDataManager<TData>();
        DataManager.OnPropertySet += EmitOnPropertySet;
    }

    public override void Reset() => OnReset();

    protected void SetDataDefaultValues() => SetDefault();

    public override byte[] SerializeData() => [(byte)(IsRunning ? 1 : 0), ..DataManager.SerializeData()];

    public override void DeserializeData(byte[] data)
    {
        if (data.Length == 0)
        {
            _isRunning = false;
            SetDataDefaultValues();
        }
        else
        {
            _isRunning = data[0] != 0;
            if (!DataManager.TryDeserializeData(data[1..])) SetDataDefaultValues();
        }
    }
}
