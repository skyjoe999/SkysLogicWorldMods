using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using LogicWorld.SharedCode.Components;
using SkysWirelessBus.Server.Wireless;
using SkysWirelessBus.Shared;
using System;
using System.Collections.Generic;

namespace SkysWirelessBus.Server.LogicCode;

public class WirelessBus : LogicComponent<IWirelessBusData>, IWireless
{
    private IWireless next = null;
    private IWireless prev = null;

    public IWireless Next { get => next; set => next = value; }
    public IWireless Prev { get => prev; set => prev = value; }
    public string ChannelName => "WirelessBus." + Data.BusName;
    public IReadOnlyList<IInputPeg> PegsList => Inputs;
    public int PegCount => Data.InputCount;
    public string _logMessage => Address.ToString();

    protected override void OnCustomDataUpdated()
    {
        if (Prev != null)
        { 
            WirelessTracker.StopTracking(this);
            QueueLogicUpdate();
        }
    }
    protected override void DoLogicUpdate() {
        if (Prev == null)
            WirelessTracker.StartTracking(this);
    }
    protected override void Initialize()
    {
        Next = null; // Shouldn't be necesary but I am tired and I would rather reset values than currupt saves...
        Prev = null;
    }
    public override void OnComponentDestroyed() => WirelessTracker.StopTracking(this);
    protected override void SetDataDefaultValues() => Data.Initialize();
    public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex) => false;
}
