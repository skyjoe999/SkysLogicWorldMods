using System;
using System.Collections;
using System.Collections.Generic;
using EccsLogicWorldAPI.Client.Hooks;
using HarmonyLib;
using JimmysUnityUtilities;
using JimmysUnityUtilities.Collections;
using LogicAPI.Client;
using LogicAPI.Networking.Packets.PartialWorlds;
using LogicWorld.Interfaces;
using LogicWorld.PartialWorlds;
using LogicWorld.SharedCode.PartialWorlds;
using SkysGeneralLib.Shared.AccessTools;
using UnityEngine;

namespace SkysAntiPartialWorldCrash.Client;

[HarmonyPatch]
public class SkysAntiPartialWorldCrash_ClientMod : ClientMod
{
    public readonly static HashSet<Guid> ActiveGuids = [];
    protected override void Initialize()
    {
        new Harmony(Manifest.ID).PatchAll();
        WorldHook.worldUnloading += ActiveGuids.Clear;
    }

    [HarmonyPatch(typeof(ClientPartialWorldsManager), nameof(ClientPartialWorldsManager.GetPartialWorldAsyncAndDoSomethingWithIt))]
    [HarmonyPrefix]
    public static bool Patch(
        ClientPartialWorldsManager __instance,
        Guid partialWorldGuid,
        PartialWorldAcquiredAction successAction,
        PartialWorldCacheDatabase ___PartialWorldCache,
        ListedDictionary<Guid, PartialWorldAcquiredAction> ___WaitingPartialWorldActions
    )
    {
        if (___PartialWorldCache.TryGetPartialWorld(partialWorldGuid, out var partialWorldData))
        {
            successAction?.Invoke(partialWorldData);
            return false;
        }

        if (!___WaitingPartialWorldActions.ContainsValuesAt(partialWorldGuid))
        {
            var packet = new PartialWorldRequestPacket { PartialWorldGuid = partialWorldGuid };
            Instances.SendData.Send(packet);
        }

        ___WaitingPartialWorldActions.AddValueAt(partialWorldGuid, successAction);
        ActiveGuids.Add(partialWorldGuid);
        CoroutineUtility.Run(TimeoutPartialWorldDownloadRoutine());
        IEnumerator TimeoutPartialWorldDownloadRoutine()
        {
            yield return new WaitForSecondsRealtime(PartialWorldDownloadTimeoutSecondsAccess.Get());
            if (!__instance.PartialWorldIsStoredLocally(partialWorldGuid) && ActiveGuids.Contains(partialWorldGuid))
                Instances.ErrorScreen.MegaTerribleCodingPractices_TriggerErrorScreen(new Exception($"Failed to download PartialWorld with guid {partialWorldGuid}"));
            ActiveGuids.Remove(partialWorldGuid);
        }
        return false;
    }

    // I dont want to add more mod dependencies so we're doing this from first principles!
    public static readonly StaticAccessor<ClientPartialWorldsManager, float> PartialWorldDownloadTimeoutSecondsAccess = new("PartialWorldDownloadTimeoutSeconds");
}
