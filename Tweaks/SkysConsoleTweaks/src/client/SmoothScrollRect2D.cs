using LogicUI.Scrolling;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkysConsoleTweaks.Client;

// I decided to make this a component instead of a patch
// Not sure if that was a mistake...
public class SmoothScrollRect2D : SmoothScrollRect
{
    public override void OnScroll(PointerEventData data)
    {
        // This does not happen by default on this old version of unity? Why???
        data.scrollDelta = Input.GetKey(KeyCode.LeftShift) ? new(data.scrollDelta.y, 0) : data.scrollDelta;
        base.OnScroll(data);
    }

    public static SmoothScrollRect2D Replace(SmoothScrollRect scrollRect)
    {
        // This is pure brute force </3
        var gameObject = scrollRect.gameObject;
        var originalData = (
            scrollRect.content, scrollRect.horizontal, scrollRect.vertical, scrollRect.movementType, scrollRect.elasticity,
            scrollRect.inertia, scrollRect.decelerationRate, scrollRect.scrollSensitivity, scrollRect.viewport,
            scrollRect.horizontalScrollbar, scrollRect.verticalScrollbar, scrollRect.horizontalScrollbarVisibility,
            scrollRect.verticalScrollbarVisibility, scrollRect.horizontalScrollbarSpacing, scrollRect.verticalScrollbarSpacing,
            scrollRect.onValueChanged, scrollRect.SmoothScrolling, scrollRect.ScrollSmoothTime, scrollRect.ScrollEaseMode
        );
        DestroyImmediate(scrollRect);
        scrollRect = gameObject.AddComponent<SmoothScrollRect2D>();
        (
            scrollRect.content, scrollRect.horizontal, scrollRect.vertical, scrollRect.movementType, scrollRect.elasticity,
            scrollRect.inertia, scrollRect.decelerationRate, scrollRect.scrollSensitivity, scrollRect.viewport,
            scrollRect.horizontalScrollbar, scrollRect.verticalScrollbar, scrollRect.horizontalScrollbarVisibility,
            scrollRect.verticalScrollbarVisibility, scrollRect.horizontalScrollbarSpacing, scrollRect.verticalScrollbarSpacing,
            scrollRect.onValueChanged, scrollRect.SmoothScrolling, scrollRect.ScrollSmoothTime, scrollRect.ScrollEaseMode
        ) = originalData;
        return (SmoothScrollRect2D)scrollRect;
    }
}
