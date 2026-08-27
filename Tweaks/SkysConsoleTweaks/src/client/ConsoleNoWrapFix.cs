using System;
using System.Linq;
using FancyPantsConsole;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicUI.MenuParts.TextResizing;
using LogicUI.Scrolling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Console = FancyPantsConsole.Console;
using Object = UnityEngine.Object;

namespace SkysConsoleTweaks.Client;

[HarmonyPatch]
public static class ConsoleNoWrapFix
{
    [HarmonyPatch(typeof(Message), nameof(Message.ShowTimestamp), MethodType.Setter)]
    [HarmonyPostfix]
    public static void TimestampFixer(Message __instance, TextMeshProUGUI ___IconText, TextMeshProUGUI ___MessageTextMesh)
    {
        ___IconText.transform.parent.GetRectTransform().SetMarginLeft(___MessageTextMesh.GetRectTransform().offsetMin.x - 10);
        ___IconText.transform.parent.GetRectTransform().sizeDelta = new(50, 0);
        ___MessageTextMesh.GetRectTransform().offsetMin += new Vector2(50, 0);
        ___MessageTextMesh.GetRectTransform().SetMarginRight(10);
        __instance.RecalculateHeight();
    }

    public static void Setup(Console console, TrackedObjectPoolUtility<Message> messagePool, SmoothScrollRect scrollRect)
    {
        var layout = scrollRect.content.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerLeft;
        scrollRect.content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var prefabField = typeof(Console).Field("MessagePrefab");
        var template = (GameObject)prefabField.GetValue(console);

        // All of this is just so we don't touch the actual prefab because that causes problems in the unity editor. (The average player will never care.)
        var templateParent = new GameObject("Message Template", typeof(RectTransform));
        templateParent.transform.SetParent(console.transform);
        prefabField.SetValue(console, template = Object.Instantiate(template, templateParent.transform)); // Set the field again just to be nice to other mod devs <3
        template.name = "Message";
        templateParent.SetActive(false);
        typeof(TrackedObjectPoolUtility<Message>).Field("CreateNewObjectInParent").SetValue(messagePool, (Func<Transform, Message>)(parent => Object.Instantiate(template, parent).GetComponent<Message>()));

        var axesField = typeof(TextAreaResizer).Field("controlAxes");
        // Setup the formatting for the messages.
        foreach (var message in messagePool.ActiveObjects.Prepend(template.GetComponent<Message>()))
        {
            var resize = message.GetComponentInChildren<ResizableText>();
            axesField.SetValue(resize, TextAreaResizer.Mode.Both);
            var text = resize.gameObject.GetComponent<TextMeshProUGUI>();
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var icon = message.transform.GetChild(2).GetRectTransform();
            SetTransformValues(icon, new(0, 0), new(0, 1), new(0, 0.5f));
            message.GetRectTransform().SetMarginRight(0);
            message.ShowTimestamp = message.ShowTimestamp; // Calls all the formatting code.
        }

        // Setup horizontal scroll bar.
        (scrollRect.horizontalScrollbar = Object.Instantiate(scrollRect.verticalScrollbar, scrollRect.verticalScrollbar.transform.parent)).name = "Scrollbar Horizontal";
        scrollRect.horizontalScrollbar.direction = Scrollbar.Direction.LeftToRight;

        scrollRect.horizontal = true;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Position it correctly.
        var hScrollTrans = scrollRect.horizontalScrollbar.GetRectTransform();
        SetTransformValues(hScrollTrans, new(0, 0), new(1, 0), new(0.5f, 0));
        hScrollTrans.sizeDelta = new(0, 25);
        hScrollTrans.localScale = Vector3.one;
        hScrollTrans.SetMarginRight(35);

        // Add space under the background for the new scrollbar.
        scrollRect.transform.GetChild(0).GetRectTransform().SetMarginBottom(30f);


        static void SetTransformValues(RectTransform transform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.pivot = pivot;
        }
    }
}
