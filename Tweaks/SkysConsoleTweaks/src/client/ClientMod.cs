using FancyPantsConsole;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicUI.Scrolling;

using Object = UnityEngine.Object;

namespace SkysConsoleTweaks.Client;

public class SkysConsoleTweaks_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        new Harmony(Manifest.ID).PatchAll();
        SetupConsole();
    }

    public static void SetupConsole()
    {
        var console = (Console)FPC.Instance;
        var pool = (TrackedObjectPoolUtility<Message>)typeof(Console).Field("MessagesPool").GetValue(console);
        var scrollRect = console.GetComponentInChildren<SmoothScrollRect>();

        // Replace the scroll rect so that holding shift scrolls horizontally.
        {
            Object.DestroyImmediate(scrollRect.GetComponent<ScrollSettingsApplier>());
            scrollRect = SmoothScrollRect2D.Replace(scrollRect);
            // One day I'll understand why unity doesn't do this for me... (that day is today, OnValidate is editor only </3)
            typeof(ScrollSettingsApplier).Field("ScrollyBoi").SetValue(scrollRect.AddComponent<ScrollSettingsApplier>(), scrollRect);
        }

        ConsoleNoWrapFix.Setup(console, pool, scrollRect);

        // Setup controller
        scrollRect.content.AddComponent<ConsoleScrollController>().Setup(console, pool);
    }
}
