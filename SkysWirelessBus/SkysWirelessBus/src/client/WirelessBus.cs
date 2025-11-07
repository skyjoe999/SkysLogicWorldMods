using JimmysUnityUtilities;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.LabelAlignment;
using LogicWorld.Interfaces;
using LogicWorld.References;
using LogicWorld.Rendering.Chunks;
using LogicWorld.Rendering.Components;
using SkysWirelessBus.Shared;
using UnityEngine;

namespace SkysWirelessBus.Client.ClientCode;
public class WirelessBus : ComponentClientCode<IWirelessBusData>, IColorableClientCode
{
        
    private LabelTextManager _label;
    private RectTransform _labelTransform;
    string IColorableClientCode.ColorsFileKey => "LabelText";
    float IColorableClientCode.MinColorValue => 0f;

    Color24 IColorableClientCode.Color
    {
        get { return Data.LabelColor; }
        set { Data.LabelColor = value; }
    }
    protected override void DataUpdate()
    {

        _label.DataUpdate(new LabelData()
        {
            LabelText = Data.BusName,
            LabelColor = Data.LabelColor,
            LabelMonospace = false,
            LabelFontSizeMax = 3f,
            HorizontalAlignment = LabelAlignmentHorizontal.Center,
            VerticalAlignment = LabelAlignmentVertical.Middle,
            SizeX = 1,
            SizeZ = 1,
        });
        if (InputCount <= 2)
        {
            _labelTransform.sizeDelta = new Vector2(2, InputCount) * 0.3f / 3f;
            SetDecorationPosition(0, new Vector3(InputCount / 3f - 1f / 6f, 2f / 3f + 0.001f, -1f / 6f) * 0.3f);
            SetDecorationRotation(0, Quaternion.Euler(90f, -90f, 0f));
        }
        else
        {
            _labelTransform.sizeDelta = new Vector2(InputCount / 3f, 2f / 3f) * 0.3f;
            SetDecorationPosition(0, new Vector3(InputCount / 3f - 1f / 6f, 2f / 3f + 0.001f, 1f / 2f) * 0.3f);
            SetDecorationRotation(0, Quaternion.Euler(90f, -180f, 0f));

        }

    }

    protected override void SetDataDefaultValues() =>Data.Initialize();
        

    protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        GameObject gameObject = Object.Instantiate(Prefabs.ComponentDecorations.LabelText, parentToCreateDecorationsUnder);
        _label = gameObject.GetComponent<LabelTextManager>();
        _labelTransform = _label.GetRectTransform();
        _labelTransform.sizeDelta = new Vector2(2, 1) * 0.3f * 0.333333f;
        return
        [
            new Decoration
            {
                LocalPosition = new Vector3(0.5f, 2f / 3f + 0.001f, -0.5f + 0.3333f) * 0.3f,
                LocalRotation = Quaternion.Euler(90f, 0f, 0f),
                DecorationObject = gameObject,
                IncludeInModels = true,
                ShouldBeOutlined = false,
            }
        ];
    }
}
public class LabelData : Label.IData
{
    private string labelText;
    private Color24 labelColor;
    private bool labelMonospace;
    private float labelFontSizeMax;
    private LabelAlignmentHorizontal horizontalAlignment;
    private LabelAlignmentVertical verticalAlignment;
    private int sizeX;
    private int sizeZ;
    public LabelData() { }
    public LabelData(string LabelText, Color24 LabelColor, bool LabelMonospace, float LabelFontSizeMax, LabelAlignmentHorizontal HorizontalAlignment, LabelAlignmentVertical VerticalAlignment, int SizeX, int SizeZ)
    {
        this.LabelText = LabelText;
        this.LabelColor = LabelColor;
        this.LabelMonospace = LabelMonospace;
        this.LabelFontSizeMax = LabelFontSizeMax;
        this.HorizontalAlignment = HorizontalAlignment;
        this.VerticalAlignment = VerticalAlignment;
        this.SizeX = SizeX;
        this.SizeZ = SizeZ;
    }

    public string LabelText { get => labelText; set => labelText = value; }
    public Color24 LabelColor { get => labelColor; set => labelColor = value; }
    public bool LabelMonospace { get => labelMonospace; set => labelMonospace = value; }
    public float LabelFontSizeMax { get => labelFontSizeMax; set => labelFontSizeMax = value; }
    public LabelAlignmentHorizontal HorizontalAlignment { get => horizontalAlignment; set => horizontalAlignment = value; }
    public LabelAlignmentVertical VerticalAlignment { get => verticalAlignment; set => verticalAlignment = value; }
    public int SizeX { get => sizeX; set => sizeX = value; }
    public int SizeZ { get => sizeZ; set => sizeZ = value; }
}
