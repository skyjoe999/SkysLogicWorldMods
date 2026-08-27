using System;
using System.Collections.Generic;
using System.Linq;
using FancyPantsConsole;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicUI.MenuParts;
using LogicUI.MenuParts.Toggles;
using LogicUI.MenuTypes.Searching;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Console = FancyPantsConsole.Console;

namespace SkysConsoleTweaks.Client;

[HarmonyPatch]
public class ConsoleScrollController : UIBehaviour, ISearchList<MessageEntry>
{
    public static ConsoleScrollController Instance;
    public Console MainConsole;

    public RectTransform MessageParent;
    public RectTransform Viewport;
    public TrackedObjectPoolUtility<Message> MessagePool;
    public RectTransform TopMargin;
    public RectTransform BottomMargin;
    public ToggleIcon TimestampToggle;
    public List<MessageEntry> Messages;
    public Searcher<MessageEntry> Searcher;

    public LayoutElement Layout; // Used for enforcing the minimum width.
    public float Spacing;

    public float BiggestWidth = 70; // Just an icon with no text.
    public float FullHeight = 0;
    public int BottomShownLine = 0;
    public int TopShownLine = 0;

    public bool ForceUpdate;
    public bool MessageAddedWhileInactive;
    public List<(int index, MessageEntry entry)> PreviousUsedEntries = [];

    public void Setup(Console console, TrackedObjectPoolUtility<Message> messagePool)
    {
        if (Messages is not null)
            throw new("Can only call setup once.");
        Messages = [];

        MessagePool = messagePool;
        Spacing = GetComponent<VerticalLayoutGroup>().spacing;
        MessageParent = (RectTransform)transform;
        Viewport = (RectTransform)MessageParent.parent;
        TimestampToggle = (ToggleIcon)typeof(Console).Field("TimestampToggle").GetValue(console);

        Layout = gameObject.AddComponent<LayoutElement>();

        TopMargin = new GameObject("Top Margin", typeof(RectTransform)).GetRectTransform();
        TopMargin.transform.SetParent(MessageParent);
        TopMargin.transform.SetAsFirstSibling();

        BottomMargin = new GameObject("Bottom Margin", typeof(RectTransform)).GetRectTransform();
        BottomMargin.transform.SetParent(MessageParent);

        BottomMargin.pivot = TopMargin.pivot = new(0.5f, 0);
        BottomMargin.sizeDelta = TopMargin.sizeDelta = new(0, -Spacing);

        var dummy = new GameObject("Dummy Search");
        dummy.transform.SetParent(TopMargin); // Just shove it in the margin to make sibling indexing easier. (Will never be rendered anyways.)
        (MessageEntry.DummyMessage = new GameObject("Dummy")).transform.SetParent(dummy.transform);
        dummy.SetActive(false); // Make the dummy have an inactive parent to maybe reduce lag from frequently update

        var oldSearch = (Searcher<Message>)typeof(Console).Field("Searcher").GetValue(console);
        typeof(Searcher<Message>).Field("SearchList").SetValue(oldSearch, new EmptySearchList());
        Searcher = new Searcher<MessageEntry>(this, (SearchBox)typeof(Searcher<Message>).Field("SearchBox").GetValue(oldSearch));

        (Instance, MainConsole) = (this, console);


        // Setup the existing messages.
        foreach (var message in MessagePool.ActiveObjects.OrderBy(message => message.transform.GetSiblingIndex()))
            AddMessage(message, GetMessageData(message));
        TimestampToggle.OnValueChanged += value => Layout.minWidth = BiggestWidth + (value ? 220 : 20);
    }

    public void Clear()
    {
        Messages.Clear();
        MessagePool.RecycleAllActiveItems(); // Should have already been called.
        BottomShownLine = TopShownLine = 0;
        TopMargin.sizeDelta = BottomMargin.sizeDelta = new(0, -Spacing);
        BiggestWidth = 70;
        Layout.minWidth = BiggestWidth + (TimestampToggle.Value ? 220 : 20);
        PreviousUsedEntries.Clear();

        Searcher.SearchListContentsChanged();
    }


    public void Update()
    {
        if (Messages.Count == 0)
            return;

        CalculateRegion(out var changed);
        if (!ForceUpdate && !changed)
            return;

        ForceUpdate = false;
        UpdateRegion();
    }

    public void CalculateRegion(out bool changed)
    {
        var topEdge = MessageParent.offsetMax.y - Viewport.rect.size.y;
        var bottomEdge = MessageParent.offsetMax.y;
        var topVisibleLine = TopShownLine;
        var bottomVisibleLine = BottomShownLine;

        while (topVisibleLine > 0)
        {
            if (Messages[topVisibleLine - 1] is { passesFilter: true } message && IsAboveFrame(message))
                break; // Ensure the one before the new first is out of frame.
            topVisibleLine--;
        }
        while (topVisibleLine < Messages.Count)
        {
            if (Messages[topVisibleLine] is { passesFilter: true } message && !IsAboveFrame(message))
                break; // Ensures new first is in frame.
            topVisibleLine++;
        }

        while (bottomVisibleLine + 1 < Messages.Count)
        {
            if (Messages[bottomVisibleLine + 1] is { passesFilter: true } message && IsBelowFrame(message))
                break; // Ensure the one after the new last is out of frame.  
            bottomVisibleLine++;
        }
        while (bottomVisibleLine > 0)
        {
            if (Messages[bottomVisibleLine] is { passesFilter: true } message && !IsBelowFrame(message))
                break; // Ensures new last is in frame (or zero).
            bottomVisibleLine--;
        }

        bool IsBelowFrame(MessageEntry message) => -message.OffsetMin - message.Height > bottomEdge;
        bool IsAboveFrame(MessageEntry message) => topEdge > -message.OffsetMin;

        // Load one extra in either direction.
        // (Watch out! This means TopShownLine and BottomShownLine are *not* guaranteed to be unfiltered)
        // ((I cannot be bothered to fix this rn but it should be possible by editing the loops above))
        bottomVisibleLine = Math.Clamp(bottomVisibleLine + 1, 0, Messages.Count - 1);
        topVisibleLine = Math.Clamp(topVisibleLine - 1, 0, Messages.Count - 1);
        changed = topVisibleLine != TopShownLine || bottomVisibleLine != BottomShownLine;
        TopShownLine = topVisibleLine;
        BottomShownLine = bottomVisibleLine;
    }

    public void UpdateRegion()
    {
        foreach (var (index, entry) in PreviousUsedEntries)
            if (index < TopShownLine || index > BottomShownLine || !entry.passesFilter)
            {
                MessagePool.Recycle(entry.message);
                entry.message = null;
            }

        PreviousUsedEntries.Clear();
        var usedMessages = new HashSet<Message>();

        var siblingIndex = TopMargin.GetSiblingIndex() + 1;
        for (int index = TopShownLine; index <= BottomShownLine; index++)
        {
            if (Messages[index] is not { passesFilter: true } entry)
                continue;

            if (entry.message == null)
                SetupMessage(entry.message = MessagePool.Get(MessageParent), entry.data, MainConsole);

            usedMessages.Add(entry.message);

            if (entry.message.transform.GetSiblingIndex() != siblingIndex)
                entry.message.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;

            PreviousUsedEntries.Add((index, entry));
        }

        BottomMargin.SetSiblingIndex(siblingIndex);

        TopMargin.sizeDelta = new(0, -PreviousUsedEntries[0].entry.TopOffset - Spacing);
        BottomMargin.sizeDelta = new(0, FullHeight - Spacing + PreviousUsedEntries.Last().entry.OffsetMin);
    }


    public void AddMessage(Message message, object data)
    {
        if (Messages is null)
            return; // Hasn't been setup yet.

        if (BiggestWidth < message.GetRectTransform().rect.size.x)
        {
            BiggestWidth = message.GetRectTransform().rect.size.x;
            Layout.minWidth = BiggestWidth + (TimestampToggle.Value ? 220 : 20);
        }

        var entry = new MessageEntry(0, message.GetRectTransform().sizeDelta.y, data, message);
        Messages.Add(entry);


        if (gameObject.activeInHierarchy) // Console is actively running.
        {
            PreviousUsedEntries.Add((Messages.Count - 1, entry));
            Searcher.SearchListContentsChanged();
        }
        else
        {
            MessageAddedWhileInactive = true;
            MessagePool.Recycle(message);
            entry.message = null;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (MessageAddedWhileInactive)
            Searcher.SearchListContentsChanged();
    }


    // This is the method that actually does the filtering since Searcher always sets the active state of a game object.
    public IEnumerable<MessageEntry> GetAllSearchItems()
    {
        var prev = 2f; // Just hardcoded... Im pretty sure this is the vertical group's spacing minus its top margin.
        FullHeight = 0;
        foreach (var entry in Messages)
        {
            yield return entry;
            if (entry.passesFilter = MessageEntry.DummyMessage.activeSelf)
            {
                prev = entry.OffsetMin = prev - entry.Height - Spacing;
                FullHeight += entry.Height + Spacing;
            }
            else
            {
                entry.OffsetMin = prev;
            }
        }
        ForceUpdate = true;
        Update();
    }

    public IEnumerable<GameObject> GetAllDependentObjects() => [];

    struct EmptySearchList : ISearchList<Message> { public readonly IEnumerable<GameObject> GetAllDependentObjects() => []; public readonly IEnumerable<Message> GetAllSearchItems() => []; }

    // Useful for printing info to ensure the console works when closed.
    [LICC.Command("Debug.ConsoleInfo", Description = "Prints relevant info about the improved console")]
    public static void PrintConsoleInfo()
    {
        // CommandBindings.add b PrintConsoleInfo
        LogicLog.LogicLogger.For<ConsoleScrollController>().Info(string.Join("\n", ["",
            $"Top: {Instance.TopShownLine}",
            $"Bottom: {Instance.BottomShownLine}",
            $"Total: {Instance.Messages.Count}",
            $"Rendered: {Instance.MessagePool.ActiveObjects.Count}",
            $"Height: {Instance.FullHeight}",
            $"Prev count: {Instance.PreviousUsedEntries.Count}",
            $"Prev: {string.Join(", ", Instance.PreviousUsedEntries.Select(pair => pair.index))}",
        ]));
    }


    #region Harmony
    [HarmonyPatch(typeof(Message), "Setup")]
    [HarmonyPostfix]
    public static void MessageInterceptor(Message __instance, object data) => Instance.AddMessage(__instance, data);

    [HarmonyPatch(typeof(Console), nameof(Console.ClearMessages))]
    [HarmonyPostfix]
    public static void ClearHook() => Instance.Clear();

    [HarmonyPatch(typeof(Message), "Setup")]
    [HarmonyReversePatch] public static void SetupMessage(Message message, object data, Console console) => throw new NotImplementedException("It's a stub");
    public static Func<Message, object> GetMessageData = typeof(Message).Field("Data").GetValue;
    #endregion
}
