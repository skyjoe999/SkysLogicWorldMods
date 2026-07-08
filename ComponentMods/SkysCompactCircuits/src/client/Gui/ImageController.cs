using EccsLogicWorldAPI.Shared;
using LogicInitializable;
using LogicWorld.Building.Subassemblies;
using LogicWorld.UI.Thumbnails;
using UnityEngine;
using UnityEngine.UI;

namespace SkysCompactCircuits.Client.Gui;

public class ImageController : MonoBehaviour, IInitializable
{
    public Vector2Int? resolution;
    private RawImage Image;
    private AspectRatioFitter AspectRatioFitter;
    private Canvas SizeReferenceCanvas;

    public void Initialize()
    {
        Image = gameObject.GetComponentInChildren<RawImage>(true);
        NullChecker.check(Image, "Could not find RawImage inside of GameObject");
        AspectRatioFitter = gameObject.GetComponentInChildren<AspectRatioFitter>(true);
        NullChecker.check(AspectRatioFitter, "Could not find AspectRatioFitter inside of GameObject");
        SizeReferenceCanvas = gameObject.GetComponentInParent<Canvas>(true);
        NullChecker.check(SizeReferenceCanvas, "Could not find Canvas inside of ancestry");
    }

    public void RenderSubassembly(SubassemblyData subassembly)
    {
        var size = resolution ?? Vector2Int.RoundToInt((SizeReferenceCanvas.transform as RectTransform).sizeDelta);
        SetTexture(ItemThumbnails.RenderSubassembly(subassembly, ItemThumbnails.ThumbnailRenderProperties.Default.WithResolution(size), RecalculateAspectRatio));
    }
    public void SetTexture(Texture2D texture)
    {
        Image.texture = texture;
        RecalculateAspectRatio();
    }
    public void RecalculateAspectRatio() =>
        AspectRatioFitter.aspectRatio = ((float)Image.texture.width) / Image.texture.height;
}
