using System;
using System.Collections.Generic;
using LogicAPI.Networking.Packets;

#if LW_SIDE_SERVER
using SkysGeneralLib.Server;
#else
using LogicWorld.Interfaces;
#endif

namespace SkysGeneralLib.Shared.Networking;

public class PacketJoiner<T>(Func<List<T>, Packet> Pack)
{
    private readonly List<T> HeldUpdates = [];
    private int PauseCount = 0;
    public event Action OnClear;
    public void Queue(T change)
    {
        if (PauseCount > 0)
            HeldUpdates.Add(change);
        else
            Send([change]);
    }

    public void PushPause() => PauseCount++;
    public void PopPause()
    {
        if (PauseCount-- > 1)
            return;
        SendQueueNow();
    }

    public void SendQueueNow()
    {
        Send(HeldUpdates);
        HeldUpdates.Clear();
        PauseCount = 0;
        OnClear?.Invoke();
    }

    public void ClearQueueWithoutSending()
    {
        HeldUpdates.Clear();
        PauseCount = 0;
        OnClear?.Invoke();
    }

    private void Send(List<T> data)
    {
        var packet = Pack(data);
        if (packet is not null)
#if LW_SIDE_SERVER
            Services.NetworkServer.Broadcast(packet);
#else
            Instances.SendData.Send(packet);
#endif
    }
}