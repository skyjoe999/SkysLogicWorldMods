using LogicAPI.Server.Components;
using System.Collections.Generic;

namespace SkysWirelessBus.Server.Wireless;
public interface IWireless
{
    IWireless Next { set; get; }
    IWireless Prev { set; get; }
    string ChannelName { get; }
    IReadOnlyList<IInputPeg> PegsList { get; }
    int PegCount { get; }
    string _logMessage { get; }
    public void InsertIntoChainBefore(IWireless node)
    {
        Next = node;
        Prev = node.Prev;
        node.Prev = this;
        Prev.Next = this;
    }
    public void LeaveChain()
    {
        Prev.Next = Next;
        Next.Prev = Prev;
        Prev = Next = null;
    }
}