using JimmysUnityUtilities;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Components;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysGeneralLib.Client.FloatingText;
using UnityEngine;

namespace SkysChallengeSystem.Client.ClientCode;

public class ChallengePeg : ComponentClientCode<IChallengePegData>, IHasChallengeBoardParent
{
    public IChallengeBoard ChallengeBoard { get; set; }


    protected override void DataUpdate()
    {
        ((FloatingText)Decorations[0]).Text = Data.PegName.IsNullOrWhiteSpace() ? "<Unset>" : Data.PegName;
    }

    protected override void InitializeInWorld() => ((IHasChallengeBoardParent)this).SetupBoard(Component.Parent);

    protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        return
        [
            new FloatingText(parentToCreateDecorationsUnder)
            {
                ShouldBeOutlined = false,
                LocalRotation = Quaternion.Euler(90, 180, 0),
                LocalPosition = new Vector3(0.5f, 1.0001f, 0) * 0.3f,
                Scale = new Vector2(1, 0.5f),
            },
        ];
    }

    public bool isQuestion => CodeInfoBools[0];

    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues(CodeInfoBools[0]);
}
