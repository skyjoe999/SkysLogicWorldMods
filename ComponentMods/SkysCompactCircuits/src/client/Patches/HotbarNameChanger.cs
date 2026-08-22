using HarmonyLib;
using LogicAPI.Data;
using LogicWorld.UI;
using SkysCompactCircuits.Shared;
using TMPro;

namespace SkysCompactCircuits.Client;

[HarmonyPatch]
public static class HotbarNameChanger
{
    [HarmonyPatch(typeof(HotbarMenu), nameof(HotbarMenu.SelectedSlot), MethodType.Setter)]
    [HarmonyPostfix]
    public static void SetSelectedHotbarItem(HotbarMenu __instance, TextMeshProUGUI ___TitleText)
    {
        try
        {
            if (__instance.SelectedItem is DetailedHotbarItemData detailed && detailed.TextID == "SkysCompactCircuits.PackedCircuit" && PackedCircuitManager.Decode(detailed.CustomData)?.Name is { Length: > 0 } name)
                ___TitleText.text = name;
        }
        catch { }
    }
}
