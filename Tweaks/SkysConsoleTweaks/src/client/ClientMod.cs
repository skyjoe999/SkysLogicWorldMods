using FancyPantsConsole;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicUI.MenuTypes;
using LogicUI.Scrolling;
using LogicWorld.UI;

using Object = UnityEngine.Object;

namespace SkysConsoleTweaks.Client;

public class SkysConsoleTweaks_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        // Set up the error screen asap.
        SetupErrorScreen();
        new Harmony(Manifest.ID).PatchAll();
        SetupConsole();
    }

    public static void SetupErrorScreen()
    {
        var errorScreen = ToggleableSingletonMenu<ErrorScreen>.Instance;
        var details = (DetailsDropdown)typeof(ErrorScreen).Field("DetailsDropdown").GetValue(errorScreen);
        var scrollRect = details.GetComponentInChildren<SmoothScrollRect>(true);

        // Replace the scroll rect so that holding shift scrolls horizontally.
        scrollRect = SwapOutScrollRectWithApplier(scrollRect);

        ErrorScreenNoWrapFix.Setup(scrollRect);
    }

    public static void SetupConsole()
    {
        var console = (Console)FPC.Instance;
        var pool = (TrackedObjectPoolUtility<Message>)typeof(Console).Field("MessagesPool").GetValue(console);
        var scrollRect = console.GetComponentInChildren<SmoothScrollRect>();

        // Replace the scroll rect so that holding shift scrolls horizontally.
        scrollRect = SwapOutScrollRectWithApplier(scrollRect);

        ConsoleNoWrapFix.Setup(console, pool, scrollRect);

        // Setup controller
        scrollRect.content.AddComponent<ConsoleScrollController>().Setup(console, pool);
    }

    private static SmoothScrollRect SwapOutScrollRectWithApplier(SmoothScrollRect scrollRect)
    {
        Object.DestroyImmediate(scrollRect.GetComponent<ScrollSettingsApplier>());
        scrollRect = SmoothScrollRect2D.Replace(scrollRect);
        // One day I'll understand why unity doesn't do this for me... (that day is today, OnValidate is editor only </3)
        typeof(ScrollSettingsApplier).Field("ScrollyBoi").SetValue(scrollRect.AddComponent<ScrollSettingsApplier>(), scrollRect);

        return scrollRect;
    }
}
