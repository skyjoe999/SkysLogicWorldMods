using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Networking.Packets.Initialization;
using LogicWorld;
using LogicWorld.UI.Thumbnails;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Shared.AccessTools;
using UnityEngine;

namespace SkysCompactCircuits.Client;

[HarmonyPatch]
public static class StartupLoader
{
    [HarmonyPatch(typeof(SceneAndNetworkManager), "HandleWorldInitializationPacket")]
    [HarmonyPrefix]
    public static void WorldInitPacketInterceptor(ref WorldInitializationPacket packet)
    {
        // This will get cleared again in a second but this clears it for worlds without the mod installed.
        PackedCircuitManager.ClearAllData();

        ClearCompactCircuitThumbnails();
        if (packet.PlayerHotbar?.HotbarItems.FirstOrDefault(isSpecialItem) is DetailedHotbarItemData { CustomData: { } fullCircuitData })
        {
            PackedCircuitManager.DeserializeData(fullCircuitData);
            if (packet.PlayerHotbar.HotbarItems.Length == 1)
                packet.PlayerHotbar = null;
            else
                packet.PlayerHotbar.HotbarItems = [.. packet.PlayerHotbar.HotbarItems.Where(item => !isSpecialItem(item))];
        }

        static bool isSpecialItem(HotbarItemData item) => item is DetailedHotbarItemData { TextID: "SkysCompactCircuits.WorldCircuitInitializationData" };
    }

    public static void ClearCompactCircuitThumbnails()
    {
        var CachedTextures = CachedTexturesAccess.Get();
        foreach (var (data, _) in CachedTextures.ToList())
            if (data.itemData is DetailedHotbarItemData detailed && detailed.TextID == "SkysCompactCircuits.PackedCircuit" && PackedCircuitManager.TryGetIndex(detailed.CustomData, out var index))
                CachedTextures.Remove(data);
    }
    private static readonly StaticAccessor<ItemThumbnails, Dictionary<(string cacheName, HotbarItemData itemData), Texture2D>> CachedTexturesAccess = new("CachedTextures");
}
