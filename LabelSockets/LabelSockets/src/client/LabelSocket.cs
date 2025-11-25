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
    private Vector2 SizeDelta;
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
        _labelTransform.sizeDelta = SizeDelta;
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
        var LocalPosition = new Vector3(
            GetBlockEntity(1).Scale.x / 2f,
            GetBlockEntity(1).Scale.y + 0.001f,
            GetBlockEntity(1).Scale.z / 2f * (CodeInfoBools[0] ? 1 : -1)
        ) + Component.ToLocalSpace(GetBlockEntity(1).WorldPosition) * 0.3f;
        Logger.Info(CodeInfoBools[0] + "");
        Quaternion LocalRotation;
        if (CodeInfoBools[0]) // Rotate
        {
            SizeDelta = new Vector2(GetBlockEntity(1).Scale.x, GetBlockEntity(1).Scale.z);
            LocalRotation = Quaternion.Euler(90f, -180f, 0f);
        }
        else
        {
            SizeDelta = new Vector2(GetBlockEntity(1).Scale.z, GetBlockEntity(1).Scale.x);
            LocalRotation = Quaternion.Euler(90f, -90f, 0f);
        }

        return
        [
            new Decoration
            {
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation,
                DecorationObject = gameObject,
                IncludeInModels = true,
                ShouldBeOutlined = false,
            }
        ];
    }
}
