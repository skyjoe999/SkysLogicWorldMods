using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.ClientCode.Resizing;
using LogicWorld.Interfaces;
using LogicWorld.Physics;
using SkysChallengeSystem.Shared.ComponentDataDefs;
using SkysBetterBoardLib.Client;
using SkysGeneralLib.Client.FloatingText;
using SkysGeneralLib.Client.TypeExtensions;
using UnityEngine;

namespace SkysChallengeSystem.Client.ClientCode;

public class ChallengeBoard : WrappedCircuitBoard<IChallengeBoardData>,
    IChallengeBoard,
    ICircuitBoardSurface,
    IResizableX,
    IResizableZ,
    IResizableCallbackReceiver
{
    int IResizableX.MinX => 2;
    int IResizableZ.MinZ => 2;

    public bool CanMoveOn(HitInfo info) => info.Hit.collider == GetBlockEntity().Collider;

    protected override void SetDataDefaultValues() => Data.SetDataDefaultValues();
    public override void OverrideCustomDataInPickedUpComponent() => Data.OverridePickedUp();

    private int previousSizeX;
    private int previousSizeZ;
    private string previousPath;

    protected override void DataUpdate()
    {
        if (!PlacedInMainWorld) DataUpdateGhost();
        base.DataUpdate();
        if (SizeX != previousSizeX || SizeZ != previousSizeZ)
        {
            previousSizeX = SizeX;
            previousSizeZ = SizeZ;
            SetBlockScale(0, new Vector3(SizeX - 1, 0.5f, SizeZ - 1));
            SetBlockScale(1, new Vector3(0.5f, 0.75f, SizeZ - 1));
            SetBlockScale(2, new Vector3(SizeX, 0.75f, 0.5f));
            SetBlockScale(3, new Vector3(0.5f, 0.75f, SizeZ - 1));
            SetBlockScale(4, new Vector3(SizeX, 0.75f, 0.5f));
            SetBlockPosition(3, new Vector3(SizeX - 0.5f, 0, 0.5f));
            SetBlockPosition(4, new Vector3(0, 0, SizeZ - 0.5f));
            ((FloatingText)Decorations[0]).Scale = new Vector2(SizeX - 1, 0.5f);
            ((FloatingText)Decorations[1]).Scale = new Vector2(SizeZ - 1, 0.5f);
            ((FloatingText)Decorations[2]).Scale = new Vector2(SizeX - 1, 0.5f);
            ((FloatingText)Decorations[3]).Scale = new Vector2(SizeZ - 1, 0.5f);
            Decorations[1].DecorationObject.transform.localPosition =
                Component.WorldPosition + new Vector3(0, 0.7501f, SizeZ - 0.5f) * 0.3f;
            Decorations[2].DecorationObject.transform.localPosition =
                Component.WorldPosition + new Vector3(SizeX - 0.5f, 0.7501f, SizeZ) * 0.3f;
            Decorations[3].DecorationObject.transform.localPosition =
                Component.WorldPosition + new Vector3(SizeX, 0.7501f, 0.5f) * 0.3f;
        }

        if (previousPath != Data.ChallengeFullPath)
        {
            previousPath = Data.ChallengeFullPath;
            var path = previousPath[(previousPath.LastIndexOf('/') + 1)..];
            ((FloatingText)Decorations[0]).Text = path;
            ((FloatingText)Decorations[1]).Text = path;
            ((FloatingText)Decorations[2]).Text = path;
            ((FloatingText)Decorations[3]).Text = path;
        }
    }

    protected override void InitializeInWorld()
    {
        foreach (var child in Component.EnumerateChildren())
            if(child.GetClientCode() is IHasChallengeBoardParent childCode) childCode.ChallengeBoard = this;
    }

    private void DataUpdateGhost()
    {
        // Board placer needs a rework
        // For now we have this
        if (SizeX < 2) SizeX = 2;
        if (SizeZ < 2) SizeZ = 2;
    }

    public void OnResizingBegin()
    {
        // Looks weird but makes resizing work
        // Need to look into resizing code to make this work/look better
        SetBlockScale(1, new Vector3(0.5f, 0.5f, SizeZ - 1));
        SetBlockScale(2, new Vector3(SizeX, 0.5f, 0.5f));
        SetBlockScale(3, new Vector3(0.5f, 0.5f, SizeZ - 1));
        SetBlockScale(4, new Vector3(SizeX, 0.5f, 0.5f));
        QueueDataUpdate();
    }

    public void OnResizingEnd()
    {
    }

    protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        return
        [
            new FloatingText(parentToCreateDecorationsUnder)
            {
                ShouldBeOutlined = false,
                LocalRotation = Quaternion.Euler(90, 0, 0),
                LocalPosition = new Vector3(0.5f, 0.7501f, 0) * 0.3f,
                Data = { LabelColor = new Color24(240, 240, 240) },
            },
            new FloatingText(parentToCreateDecorationsUnder)
            {
                ShouldBeOutlined = false,
                LocalRotation = Quaternion.Euler(90, 0, -90),
                LocalPosition = new Vector3(0, 0.7501f * 0.3f, 0),
                Data = { LabelColor = new Color24(240, 240, 240) },
            },
            new FloatingText(parentToCreateDecorationsUnder)
            {
                ShouldBeOutlined = false,
                LocalRotation = Quaternion.Euler(90, 0, 180),
                LocalPosition = new Vector3(0, 0.7501f * 0.3f, 0),
                Data = { LabelColor = new Color24(240, 240, 240) },
            },
            new FloatingText(parentToCreateDecorationsUnder)
            {
                ShouldBeOutlined = false,
                LocalRotation = Quaternion.Euler(90, 0, 90),
                LocalPosition = new Vector3(0, 0.7501f * 0.3f, 0),
                Data = { LabelColor = new Color24(240, 240, 240) },
            },
        ];
    }

    public string ChallengeFullPath
    {
        get => Data.ChallengeFullPath;
        set => Data.ChallengeFullPath = value;
    }
}

public interface IChallengeBoard
{
    string ChallengeFullPath { get; set; }
}

public interface IHasChallengeBoardParent
{
    IChallengeBoard ChallengeBoard { get; set; }

    void SetupBoard(ComponentAddress parent)
    {
        var board = parent;
        while (
            board != ComponentAddress.Empty &&
            board.GetClientCode(out var component) is not IChallengeBoard
        ) board = component.Parent;
        ChallengeBoard = board.GetClientCode() as IChallengeBoard;
    }
}
