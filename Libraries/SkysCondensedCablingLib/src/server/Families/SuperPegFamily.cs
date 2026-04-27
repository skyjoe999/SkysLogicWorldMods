using System.Collections;
using System.Collections.Generic;
using JimmysUnityUtilities;

namespace SkysCondensedCablingLib.Server;

public class SuperPegFamily(
    IReadOnlyDictionary<int, (Color24 Off, Color24 On)> colorByBitCount = null,
    bool canWireToStandard = true,
    (Color24 Off, Color24 On)? defaultColor = null
) : IReadOnlyDictionary<int, (Color24 Off, Color24 On)>
{
    private readonly IReadOnlyDictionary<int, (Color24 Off, Color24 On)> ColorByBitCount = colorByBitCount ?? new Dictionary<int, (Color24 Off, Color24 On)>();
    public readonly bool CanWireToStandard = canWireToStandard;
    public readonly int? ConnectionID = canWireToStandard ? null : NextID++;
    public readonly (Color24 Off, Color24 On)? DefaultColor = defaultColor;

    private static int NextID = 1;

    public static readonly SuperPegFamily Standard;
    static SuperPegFamily()
    {
        var standard = new Dictionary<int, (Color24 Off, Color24 On)>()
        {
            [2] = (new(0x755134), new(0xEB781C)),
            [4] = (new(0x928234), new(0xEBC71C)),
            [8] = (new(0x1B7B3C), new(0x00FF57)),
            [16] = (new(0x2D776D), new(0x1CEBD1)),
            [32] = (new(0x355772), new(0x309FF7)),
            [64] = (new(0x293671), new(0x2245DB)),
            [128] = (new(0x372A7D), new(0x5839F4)),
            [256] = (new(0x3F2B72), new(0x6C3BEC)),
            [3] = (new(0x892D47), new(0xFF2D68)),
        };
        standard[5] = standard[16];
        standard[6] = standard[32];
        standard[7] = standard[64];
        standard[12] = standard[128];
        standard[24] = standard[256];
        standard[48] = standard[3];
        standard[96] = standard[4];

        Standard = new(standard, defaultColor: (new(0x632626), new(0xCD2323)));
    }

    public IEnumerable<int> Keys => ColorByBitCount.Keys;
    public IEnumerable<(Color24 Off, Color24 On)> Values => ColorByBitCount.Values;
    public int Count => ColorByBitCount.Count;

    public (Color24 Off, Color24 On) this[int key] => ColorByBitCount.TryGetValue(key, out var value) ? value : DefaultColor ?? throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
    public bool ContainsKey(int key) => key > 0 && DefaultColor is not null || ColorByBitCount.ContainsKey(key);
    public bool TryGetValue(int key, out (Color24 Off, Color24 On) value) => ColorByBitCount.TryGetValue(key, out value) || (value = DefaultColor ?? default) == DefaultColor;

    public IEnumerator<KeyValuePair<int, (Color24 Off, Color24 On)>> GetEnumerator() => ColorByBitCount.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
