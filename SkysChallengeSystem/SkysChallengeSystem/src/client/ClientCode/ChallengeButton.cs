using LogicUI.MenuParts;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Client.FloatingText;
using UnityEngine;

namespace SkysChallengeSystem.Client.ClientCode;

public class ChallengeButton : GenericButton<IChallengeButtonData>
{
    private FloatingText IconObject;
    private GameObject ButtonObject;

    protected override void DataUpdate()
    {
        var color = Data.ButtonType.ToColor();
        if (color != Data.ButtonColor)
            Data.ButtonColor = color;

        base.DataUpdate();

        IconObject.Text = FontIcon.UnicodeToChar(Data.ButtonType.ToIcon());
        IconObject.Data.LabelColor = Data.ButtonType.ToIconColor();
        IconObject.DecorationObject.transform.SetParent(ButtonObject.transform);
    }

    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues();

    protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        var decorations = base.GenerateDecorations(parentToCreateDecorationsUnder);
        ButtonObject = decorations[0].DecorationObject;
        IconObject = new FloatingText(parentToCreateDecorationsUnder)
        {
            Scale = new Vector2(0.7f, 0.7f),
            Text = FontIcon.UnicodeToChar(Data.ButtonType.ToIcon()),
            LocalRotation = Quaternion.Euler(90, 180, 0),
            LocalPosition = new Vector3(0.35f, 1.2001f, 0.35f) * 0.3f,
            IncludeInModels = true,
            ShouldBeOutlined = false,
            AutoSetupColliders = false,
            Data =
            {
                LabelFontSizeMax = 2f,
                LabelColor = Data.ButtonType.ToIconColor(),
            }
        };
        return [..decorations, IconObject];
    }
}
