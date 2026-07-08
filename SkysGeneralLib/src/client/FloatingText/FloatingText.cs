using JimmysUnityUtilities;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.LabelAlignment;
using LogicWorld.References;
using LogicWorld.Rendering.Chunks;
using LogicWorld.SharedCode.BinaryStuff;
using UnityEngine;

namespace SkysGeneralLib.Client.FloatingText;

public class FloatingText : Decoration
{
    protected LabelTextManager TextManager;
    private readonly CustomDataManager<Label.IData> DataManager;
    public Label.IData Data => DataManager.Data;

    private Vector2 _Scale = new Vector2(1, 1);

    public Vector2 Scale
    {
        get => _Scale;
        set
        {
            _Scale = value;
            DataUpdate();
        }
    }

    public FloatingText(Transform parentToCreateDecorationsUnder)
    {
        DecorationObject = Object.Instantiate(Prefabs.ComponentDecorations.LabelText, parentToCreateDecorationsUnder);
        TextManager = DecorationObject.GetComponent<LabelTextManager>();
        DataManager = new()
        {
            Data =
            {
                HorizontalAlignment = LabelAlignmentHorizontal.Center,
                LabelColor = new(27, 27, 27),
                LabelFontSizeMax = 0.8f,
                LabelMonospace = false,
                LabelText = "Unset",
                SizeX = 1,
                SizeZ = 1,
                VerticalAlignment = LabelAlignmentVertical.Middle
            }
        };
        DataManager.OnPropertySet += DataUpdate;
    }

    protected virtual void DataUpdate()
    {
        TextManager.DataUpdate(DataManager.Data);
        DecorationObject.GetRectTransform().sizeDelta = Scale *0.3f;
    }

    public virtual void Clear()
    {
        Data.LabelText = "";
    }
    public virtual void WriteLine(string line)
    {
        if (Data.LabelText != "") Data.LabelText += "\n";
        Data.LabelText += line;
    }

    public string Text {
        get => Data.LabelText;
        set => Data.LabelText = value;
    }
}
