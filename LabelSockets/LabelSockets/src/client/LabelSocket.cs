using JimmysUnityUtilities;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.LabelAlignment;
using LogicWorld.Interfaces;
using LogicWorld.References;
using LogicWorld.Rendering.Chunks;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode;
using UnityEngine;

namespace LabelSockets.Client.ClientCode;

public class LabelSocket : ComponentClientCode<Label.IData>, IColorableClientCode
{
    private LabelTextManager _label;
    private RectTransform _labelTransform;
    private float Height => 0.666666f;
    string IColorableClientCode.ColorsFileKey => "LabelText";
    float IColorableClientCode.MinColorValue => 0f;

    Color24 IColorableClientCode.Color
    {
        get => Data.LabelColor;
        set => Data.LabelColor = value;
    }

    protected override void DataUpdate()
    {
        _label.DataUpdate(Data);
        _labelTransform.sizeDelta = new Vector2(2, 1) * 0.3f * 0.333333f;
    }

    protected override void SetDataDefaultValues()
    {
        Data.LabelText = "Text";
        Data.LabelFontSizeMax = 0.8f;
        Data.LabelColor = new Color24(38, 38, 38);
        Data.LabelMonospace = false;
        Data.HorizontalAlignment = LabelAlignmentHorizontal.Center;
        Data.VerticalAlignment = LabelAlignmentVertical.Middle;
        Data.SizeX = 1;
        Data.SizeZ = 1;
    }

    protected override void FrameUpdate()
    {
        var color = (GetInputState() ? Colors.SnappingPegOn : Colors.SnappingPegOff);
        SetBlockColor(color);
    }

    protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        var gameObject = Object.Instantiate(Prefabs.ComponentDecorations.LabelText, parentToCreateDecorationsUnder);
        _label = gameObject.GetComponent<LabelTextManager>();
        _labelTransform = _label.GetRectTransform();
        _labelTransform.sizeDelta = new Vector2(2, 1) * 0.3f * 0.333333f;
        return
        [
            new Decoration
            {
                LocalPosition = new Vector3(0.5f - 0.3333f, Height + 0.001f, -0.5f + 0.3333f) * 0.3f,
                LocalRotation = Quaternion.Euler(90f, -90f, 0f),
                DecorationObject = gameObject,
                IncludeInModels = true,
                ShouldBeOutlined = false,
            }
        ];
    }
}
