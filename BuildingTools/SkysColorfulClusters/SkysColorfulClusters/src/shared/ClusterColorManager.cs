using System;
using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Data;
using LogicWorld.SharedCode;
using LogicWorld.SharedCode.BinaryStuff;

#if LW_SIDE_SERVER
using SkysGeneralLib.Server.TypeExtensions;
#else
using SkysGeneralLib.Client.TypeExtensions;
#endif

namespace SkysColorfulClusters.Shared;

static class ClusterColorManager
{
    public static readonly Dictionary<int, ClusterColorEntry> ClusterColors = [];
    
    public static ClusterColor? GetClusterColor(PegAddress pegAddress) => GetClusterColor(pegAddress.GetStateID());
    public static ClusterColor? GetClusterColor(int stateID) =>
        ClusterColors.TryGetValue(stateID, out var result) ? new(result.Off, result.On) : null;
    public static Color24? GetClusterColor(int stateID, bool on) =>
        GetClusterColor(stateID) is ClusterColor color ? color.ForState(on) : null;

    public static void ClearClusterColor(PegAddress pegAddress) => ClearClusterColor(pegAddress.GetStateID());
    public static void ClearClusterColor(int stateID)
    {
        ClusterColors.Remove(stateID);
        ClusterColorSet.Invoke(stateID, null);
    }

    public static void SetClusterColor(ClusterColorEntry data) => SetClusterColor(data.Address, data.Color);
    public static void SetClusterColor(PegAddress pegAddress, Color24 off, Color24 on) => SetClusterColor(pegAddress, new ClusterColor(off, on));
    public static void SetClusterColor(PegAddress pegAddress, (Color24 Off, Color24 On)? color) => SetClusterColor(pegAddress, color.HasValue ? new ClusterColor(color.Value.Off, color.Value.On) : null);
    public static void SetClusterColor(PegAddress pegAddress, ClusterColor? color)
    {
        if (color is ClusterColor _color)
            SetClusterColorUnsafe(pegAddress.GetStateID(), new(pegAddress, _color));
        else if (pegAddress.IsNotEmpty())
            SetClusterColorUnsafe(pegAddress.GetStateID(), null);
    }
    public static void SetClusterColorUnsafe(int stateID, PegAddress pegAddress, ClusterColor? color) => SetClusterColorUnsafe(stateID, color.HasValue ? new(pegAddress, color.Value) : null);
    public static void SetClusterColorUnsafe(int stateID, ClusterColorEntry? data)
    {
        if (data is not ClusterColorEntry _data)
            ClearClusterColor(stateID);
        else
        {
            ClusterColors[stateID] = _data;
            ClusterColorSet.Invoke(stateID, _data);
        }
    }
    public static ClusterColorEntry? GetClusterData(int stateID) => ClusterColors.TryGetValue(stateID, out var result) ? result : null;
    public static PegAddress? GetPrimaryPeg(int stateID) => ClusterColors.TryGetValue(stateID, out var result) ? result.Address : null;
    public static bool IsPrimaryPeg(PegAddress pegAddress) => GetPrimaryPeg(pegAddress.GetStateID()) == pegAddress;

    [Command("ColorfulClusters.ShowTracked", Description = "Prints the list of colored clusters and their primary pegs that are currently being tracked by either the client or server.")]
    private static void ShowTrackedColoredCluster()
    {
        foreach ((var stateID, (var address, var off, var on)) in ClusterColors)
            LConsole.WriteLine($"{stateID}: (address: {address}, off: {off}, on: {on})");
    }

    public static event Action<int, ClusterColorEntry?> ClusterColorSet;

    /// <summary> Clears out all of the tracked colors. </summary>
    /// <returns> The list of state ids before reset. </returns>
    public static IEnumerable<int> ClearAll()
    {
        var keys = ClusterColors.Select(p => p.Key).ToArray();
        ClusterColors.Clear();
        return keys;
    }

    public static void DeserializeData(byte[] data)
    {
        ClusterColors.Clear();

        const int DataSize = 9 + 3 + 3;
        using var memoryByteReader = new MemoryByteReader(data);
        for (var i = 0; i < data.Length / DataSize; i++)
        {
            var peg = memoryByteReader.ReadPegAddress();
            var off = memoryByteReader.ReadColor24();
            var on = memoryByteReader.ReadColor24();
            if (peg.GetStateIDOrNull() is int stateID)
                ClusterColors[stateID] = new(peg, off, on);
        }
    }
    public static byte[] SerializeData(int expectedDataLength = 16)
    {
        var byteWriter = new ByteWriter(expectedDataLength);
        foreach ((_, var info) in ClusterColors)
            byteWriter.Write(info.Address).Write(info.Off).Write(info.On);

        return byteWriter.Finish();
    }
}

public record struct ClusterColorEntry(PegAddress Address, Color24 Off, Color24 On) : IEquatable<ClusterColorEntry>
{
    public ClusterColorEntry(PegAddress Address, ClusterColor Color) : this(Address, Color.Off, Color.On) { }
    public readonly ClusterColor Color => new(Off, On);
    public readonly bool Equals(ClusterColorEntry entry) => Address.Equals(entry.Address) && Off.Equals(entry.Off) && On.Equals(entry.On);

    public override readonly int GetHashCode() => HashCode.Combine(Address, Off, On);
}
public record struct ClusterColor(Color24 Off, Color24 On) : IEquatable<ClusterColor>
{
    public readonly bool Equals(ClusterColor entry) => Off.Equals(entry.Off) && On.Equals(entry.On);

    public readonly Color24 ForState(bool on) => on ? On : Off;

    public override readonly int GetHashCode() => HashCode.Combine(Off, On);

    public static readonly ClusterColor Default = new(Colors.CircuitColor24(false), Colors.CircuitColor24(true));
    public readonly bool IsDefault() => this == Default;
}