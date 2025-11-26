using FancyInput;
using LogicAPI.Client;
using SkysBoardTools.Client.Keybindings;

namespace SkysBoardTools.Client;

public class SkysBoardTools_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        CustomInput.Register<SkysBoardToolsContext, SkysBoardToolsTrigger>("SkysBoardTools");
        // var harmony = new Harmony("SkysSkysBoardTools");
        // // Harmony.DEBUG = true;
        // harmony.PatchAll();
    }
}

// My debugging tools are going to git because I am tired of remaking them!

// [HarmonyPatch]
// static class Patch6
// {
//     static IEnumerable<MethodBase> TargetMethods()
//     {
//         return
//         [
//             typeof(CircuitBoard).GetMethod("GenerateDecorations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
//         ];
//     }
//     static bool Prefix(CircuitBoard __instance, Transform parentToCreateDecorationsUnder, out IDecoration[] __result)
//     {
//         __result = [
//             new FloatingText(parentToCreateDecorationsUnder){
//                 LocalPosition = new Vector3(0, 0.5001f, 0) *0.3f,
//                 LocalRotation = Quaternion.Euler(90,0,0),
//                 Data = { LabelColor = new Color24(255, 0 ,0)}
//             }
//         ];
//         return false;
//     }
//     public static void SetText(this CircuitBoard instance, string text)
//     {
//         (instance.Decorations[0] as FloatingText)!.Text = text;
//     }
// }
