using LogicWorld.ClientCode.Resizing;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Components;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Client.FloatingText;
using UnityEngine;

namespace SkysChallengeSystem.Client.ClientCode;

public class ChallengeDisplay :
    ComponentClientCode<IChallengeDisplayData>,
    IResizableX,
    IResizableY,
    IHasChallengeBoardParent
{
    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues();

    private int previousSizeX;
    private int previousSizeY;

    protected override void DataUpdate()
    {
        if (previousSizeX != SizeX || previousSizeY != SizeY)
        {
            previousSizeX = SizeX;
            previousSizeY = SizeY;
            SetBlockScale(0, new Vector3(SizeX, SizeY, 0.3333333f));
            SetBlockScale(1, new Vector3(SizeX, 1, 0.6666666f));
            ((FloatingText)Decorations[0]).Scale = new Vector2(SizeX, SizeY);
            Decorations[0].DecorationObject.transform.localPosition =
                Component.WorldPosition + new Vector3(SizeX - 0.5f, 0, 0.5001f) * 0.3f;
        }

        if (Data.DisplayText == IChallengeDisplayData.SuperSecretTemporaryString)
        {
            if (Data.IsError) Data.IsError = false;
            ((FloatingText)Decorations[0]).Text =
                ChallengeManager.GetRecord(ChallengeBoard?.ChallengeFullPath ?? "")?
                    .Description?.Replace("\r", "") ?? "";
        }
        else
            ((FloatingText)Decorations[0]).Text = Data.DisplayText ?? "";

        ((FloatingText)Decorations[0]).Data.LabelFontSizeMax = Data.FontSize;
        ((FloatingText)Decorations[0]).Data.LabelColor = Data.ToColor();
    }

    protected override void InitializeInWorld() => ((IHasChallengeBoardParent)this).SetupBoard(Component.Parent);

    protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        return
        [
            new FloatingText(parentToCreateDecorationsUnder)
            {
                ShouldBeOutlined = false,
                LocalRotation = Quaternion.Euler(0, 180, 0),
                LocalPosition = new Vector3(-0.5f, 0, 0.5001f) * 0.3f,
                Scale = new Vector2(1, 2 / 3f),
            },
        ];
    }

    public int MinX => 1;
    public int MaxX => 8;
    public float GridIntervalX => 1;
    public int MinY => 1;
    public int MaxY => 8;
    public float GridIntervalY => 1;

    public int SizeX
    {
        get => Data.SizeX;
        set => Data.SizeX = value;
    }

    public int SizeY
    {
        get => Data.SizeY;
        set => Data.SizeY = value;
    }

    public IChallengeBoard ChallengeBoard { get; set; }
}