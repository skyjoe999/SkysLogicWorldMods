using System.Collections.Generic;
using FancyPantsConsole;
using LogicUI.MenuTypes.Searching;
using UnityEngine;

namespace SkysConsoleTweaks.Client;

public class MessageEntry(float offsetMin, float height, object data, Message message) : ISearchableItem
{
    public float Height = height;
    public float OffsetMin = offsetMin;
    public float TopOffset => OffsetMin + Height;

    public object data = data;
    public Message message = message;
    public bool passesFilter = true;

    public static GameObject DummyMessage;

    // We need to copy these because the messages get reused.
    public IReadOnlyList<string> NonLocalizedTags { get; } = [.. message.NonLocalizedTags];
    public IReadOnlyList<string> NonLocalizedMatchFullTags { get; } = [.. message.NonLocalizedMatchFullTags];
    public IReadOnlyList<string> LocalizedTags => null;
    public IReadOnlyList<string> CustomLocalizedTagCollectionKeys => null;
    public GameObject gameObject => DummyMessage;
    public IReadOnlyList<GameObject> DependentObjects => null;
}
