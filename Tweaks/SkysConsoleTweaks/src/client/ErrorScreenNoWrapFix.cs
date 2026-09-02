using HarmonyLib;
using JimmysUnityUtilities;
using LogicUI.Layouts.Controllers;
using LogicUI.MenuParts.TextResizing;
using LogicUI.Palettes;
using LogicUI.Scrolling;
using ThisOtherThing.UI.Shapes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace SkysConsoleTweaks.Client;

public static class ErrorScreenNoWrapFix
{
    public static void Setup(SmoothScrollRect scrollRect)
    {
        var oldBackground = scrollRect.GetComponentInChildren<Rectangle>(true);
        var outline = oldBackground.GetComponentInChildren<PaletteRectangleOutline>(true);

        // We cannot use the original background because it would hide/go under the scroll bar.
        // So instead we take the outline and make it a second background.
        oldBackground.ShapeProperties.DrawFill = false;
        outline.GetRectTransform().offsetMin += new Vector2(0, 35); // Need the added space for the scroll bar.
        var background = Object.Instantiate(outline.GetComponent<Rectangle>(), outline.transform.parent);
        background.transform.SetAsFirstSibling();

        background.ShapeProperties.DrawFill = true;
        background.ShapeProperties.DrawOutline = false;
        var backgroundColor = background.AddComponent<PaletteGraphic>();
        typeof(PaletteGraphic).Field("Target").SetValue(backgroundColor, background);
        backgroundColor.SetPaletteColor(PaletteColor.Secondary); // Hardcoded, sue me.
        backgroundColor.name = "Background";

        oldBackground.GetComponent<GrowElementListLayout>().Spacing = 10;

        // Setting up the scroll bar.
        scrollRect.horizontalScrollbar = Object.Instantiate(scrollRect.verticalScrollbar, oldBackground.transform);
        scrollRect.horizontalScrollbar.direction = Scrollbar.Direction.LeftToRight;
        scrollRect.horizontalScrollbar.name = "Scrollbar Horizontal";
        scrollRect.horizontalScrollbar.GetComponent<LayoutElement>().minHeight = 25;
        scrollRect.horizontal = true;
        // Because of the background hackery hiding it is worse </3
        // (Not that any error ever isn't going to need horizontal scrolling)
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Obviously we need to turn off line wrap, that's the whole goal.
        var text = scrollRect.content.GetComponentInChildren<TextMeshProUGUI>();
        text.textWrappingMode = TextWrappingModes.NoWrap;

        // Originally, the viewport set its preferred size based on the content and the content's width to match the viewport.
        // This means the content's width was limited which effectively prevents horizontal scrolling.
        Object.Destroy(scrollRect.viewport.GetComponent<LayoutGroup>());
        Object.Destroy(scrollRect.content.GetComponent<LayoutGroup>());

        // We want the size of the content box to be dependent on the size of the text.
        var resize = text.AddComponent<ResizableText>();
        typeof(ResizableText).Field("textmesh").SetValue(resize, resize.GetComponent<TMP_Text>());
        typeof(ResizableText).Field("_ResizingTarget").SetValue(resize, scrollRect.content.GetRectTransform());

        // We set the viewport's preferred size because we need it to expand but still be constrained by the layout.
        var layout = scrollRect.viewport.AddComponent<LayoutElement>();
        // (Why doesn't Vector2 have a deconstruct method??? TT)
        resize.OnRecalculateSize += () => (layout.preferredWidth, layout.preferredHeight) = (scrollRect.content.GetRectTransform().rect.width, scrollRect.content.GetRectTransform().rect.height);

        // Setup the text box sizing (it was messed up by the layout group).
        var textTransform = text.GetRectTransform();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.pivot = Vector2.one / 2f;
        textTransform.offsetMax = -(textTransform.offsetMin = new(25, 10));
    }
}
