using EccsGuiBuilder.Client.Components;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.RootWrappers;
using EccsLogicWorldAPI.Client.UnityHelper;
using EccsLogicWorldAPI.Shared;
using SkysGeneralLib.Shared.AccessTools;
using UnityEngine;
using UnityEngine.UI;

namespace SkysCompactCircuits.Client.Gui;

// I should make a pull request...
public static class CustomWS
{
    public static GameObject root;
    public static void init()
    {
        if (root != null)
            return;

        root = new StaticAccessor<GameObject>(typeof(VanillaStore), "root").Get();
        var BigImage = GameObjectQuery.queryGameObject("Save Subassembly Menu", "Menu", "Menu Content", "Top", "Left side", "Big Image");
        NullChecker.check(BigImage, "Could not find Big image in Save Subassembly Menu");
        BigImage.name = "Image border";
        stockImage = BigImage.cloneWithParent(root);
        stockImage.setParent(root);
        
        stockWordlessToggle = WS.toggle.gameObject.transform.GetChild(0).gameObject;
        var toggleLayout = stockWordlessToggle.AddComponent<LayoutElement>();
        toggleLayout.minHeight = toggleLayout.preferredHeight = 50;
        toggleLayout.minWidth = toggleLayout.preferredWidth = 50 * 1.7f;
        stockWordlessToggle.setParent(root);
    }

    private static GameObject stockImage;
    private static GameObject stockWordlessToggle;
    public static GameObject genStockImage => stockImage.clone();
    public static GameObject genWordlessToggle => stockWordlessToggle.clone();

    public static SimpleWrapper image => new(genStockImage);
    public static SimpleWrapper wordlessToggle => new(genWordlessToggle);

    // no clue why these are ending up inactive (maybe I'm missing something?)
    public static WindowWrapper FixResizingArrows(this WindowWrapper window)
    {
        var resizing = window.gameObject.getChild(1).getChild(4);
        for (int i = 0; i < resizing.transform.childCount; i++)
            resizing.getChild(i).SetActive(true);
        return window;
    }
}
