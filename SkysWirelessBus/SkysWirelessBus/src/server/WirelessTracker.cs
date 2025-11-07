using LICC;
using LogicAPI.Server.Components;
using System.Collections.Generic;

namespace SkysWirelessBus.Server.Wireless;
public static class WirelessTracker
{
    private static readonly Dictionary<(string, int), (IWireless,IWireless)> ChannelDict = [];
    
    [Command("WirelessTracking", Description = "List all currently tracked wireless connections.")]
    public static void WirelessTracking()
    {
        LConsole.WriteLine(ChannelDict.Count.ToString() + " channels found");
        foreach (var key in ChannelDict.Keys)
        {
            var (name, pegCount) = key;
            var (first, last) = ChannelDict[key];
            int count = 1;
            string message = first._logMessage;
            while (first != last)
            {
                first = first.Next;
                message += " -> " + first._logMessage;
                count++;
            }
            LConsole.WriteLine(name + ":" + pegCount.ToString() + " Lenght = " + count.ToString());
            LConsole.WriteLine(message);
        }
    }

    // Honestly, I'm just trusting you to not mess this up
    // No error handling, we segfault like men!
    // (Assumes router is not in the dict already)
    public static void StartTracking(IWireless router)
    {
        var key = (router.ChannelName, router.PegCount);
        if (!ChannelDict.TryGetValue(key, out var value))
            value = CreateChannel(key);
        var (first, last) = value;
        router.InsertIntoChainBefore(last);
        if (router.Prev != first)
            Connect(router.Prev, router);
        //LConsole.WriteLine("Connecting: " + router.Prev._logMessage + " -> " + router._logMessage + " -> " + router.Next._logMessage);
    }
    public static void StopTracking(IWireless router)
    {
        if (router.Prev == null)
            return;
        //LConsole.WriteLine("Removing: " + router.Prev._logMessage + " -> " + router._logMessage + " -> " + router.Next._logMessage);
        var prev = router.Prev;
        var next = router.Next;
        router.LeaveChain();
        Clear(router);
        if (prev is WirelessChainEnd && next is WirelessChainEnd)
        {
            DeleteChannel(((string ChannelName, int PegCount))(prev.ChannelName, prev.PegCount)); ;
            return;
        }
        if (prev is not WirelessChainEnd && next is not WirelessChainEnd)
            Connect(prev, next);
    }
    public static void UpdateTracking(IWireless router)
    {
        StopTracking(router);
        StartTracking(router);
        //LConsole.WriteLine("Updating: " + router.Prev._logMessage + " -> " + router._logMessage + " -> " + router.Next._logMessage);
    }
    private static void Connect(IWireless router, IWireless other)
    {
        var routerPegsList = router.PegsList;
        var otherPegsList = other.PegsList;
        var count = int.Min(router.PegCount, other.PegCount);
        for (int i = 0; i < count; i++)
            routerPegsList[i].AddSecretLinkWith(otherPegsList[i]);
    }
    private static void Clear(IWireless router)
    {
        var routerPegsList = router.PegsList;
        for (int i = 0; i < routerPegsList.Count; i++)
            routerPegsList[i].RemoveAllSecretLinks();
    }
    private static (IWireless, IWireless) CreateChannel((string, int) key)
    {
        var value = (new WirelessChainEnd(key), new WirelessChainEnd(key));
        var (first, last) = value;
        first.Next = last;
        last.Prev = first;
        ChannelDict.Add(key, value);
        return value;
    }
    private static void DeleteChannel((string, int) key) => ChannelDict.Remove(key);
    public class WirelessChainEnd(string ChannelName, int PegCount) : IWireless
    {
        private IWireless next = null;
        private IWireless prev = null;
        private readonly string channelName = ChannelName;
        private readonly int pegCount = PegCount;

        public WirelessChainEnd((string, int) key) : this(key.Item1, key.Item2) { }

        public IWireless Next { get => next; set => next = value; }
        public IWireless Prev { get => prev; set => prev = value; }
        public string ChannelName { get => channelName; }
        public IReadOnlyList<IInputPeg> PegsList => null;
        public int PegCount => pegCount;
        public string _logMessage => "end";
    }
}