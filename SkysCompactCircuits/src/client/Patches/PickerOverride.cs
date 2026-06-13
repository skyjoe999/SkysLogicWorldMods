using EccsLogicWorldAPI.Shared.AccessHelper;
using HarmonyLib;
using LogicAPI.Data;
using LogicWorld.Building;
using SkysCompactCircuits.Client.ClientCode;
using SkysGeneralLib.Client.TypeExtensions;
using SkysGeneralLib.Shared.AccessTools;

namespace SkysCompactCircuits.Client;

[HarmonyPatch("ComponentPicker", "PickComponent")]
public static class PickerOverride
{
    private static bool? state;
    public static void Prefix(ComponentAddress cAddress, ref bool pickDetail)
    {
        state = null;
        if (cAddress.GetClientCode() is PackedCircuit)
        {
            pickDetail = true;
            state = AllowPickingIllegalComponentsAccess.Get();
            AllowPickingIllegalComponentsAccess.Set(true);
        }
    }
    public static void Postfix()
    {
        if (state.HasValue)
            AllowPickingIllegalComponentsAccess.Set(state.Value);
    }
    private static readonly StaticAccessor<bool> AllowPickingIllegalComponentsAccess = new(Types.findInAssembly(typeof(WireGhost), "LogicWorld.Building.ComponentPicker"), "AllowPickingIllegalComponents");
}
