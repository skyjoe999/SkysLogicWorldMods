using HarmonyLib;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld.Building.Overhaul;
using LogicWorld.Interfaces;
using LogicWorld.Physics;

namespace SkysBetterBoardLib.Client;

public class SkysBetterBoardLib_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        var harmony = new Harmony("SkysBetterBoardLibClient");
        // Harmony.DEBUG = true;
        harmony.PatchAll();
    }
}
// ReSharper disable All
[HarmonyPatch(typeof(StuffPlacer), nameof(StuffPlacer.CanMoveOn))]
class Patch
{

    static bool Prefix(HitInfo info, out bool __result)
    {
        __result = false;
        if (info.cAddress == ComponentAddress.Empty)
            return true;
        IComponentClientCode clientCode = Instances.MainWorld.Renderer.Entities.GetClientCode(info.cAddress);
        if (clientCode is not SemiCircuitBoard)
            return true;
        __result = (clientCode as SemiCircuitBoard).CanMoveOn(info);
        return false;
    }
}
