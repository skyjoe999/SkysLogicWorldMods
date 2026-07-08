using System;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Chunks;
using LogicWorld.SharedCode.ComponentCustomData;
using LogicWorld.SharedCode.Components;
using SkysGeneralLib.Shared.AccessTools;
using UnityEngine;

namespace SkysCompactCircuits.Client.Addons;

public class SwitchAddon(Color24 SwitchColor, bool StartOn) : SuperAddon<Switch>
{
    protected override void Initialize()
    {
        base.Initialize();
        WorldPositionRotationCalculatedAccess.Set((ComponentDataManager)Inner.Component, true);
        WorldRotationAccess.Set((ComponentDataManager)Inner.Component, VisualSwitchAccess.Get(Inner).transform.rotation * Quaternion.Euler(StartOn ? 40f : -40f, 0f, 0f).Inverse());
        CallDataUpdate(Inner, []);
        HasBeenFullyInitializedAccess.Set(Inner, true);
    }

    public override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        Inner = new();
        DummyEntity.Scale = Vector3.one * 0.3f;
        BlockEntitiesAccess.Set(Inner, [DummyEntity]);
        var decorations = (IDecoration[])RunGenerateDecorations(Inner, [parentToCreateDecorationsUnder]);
        DecorationsAccess.Set(Inner, decorations);

        decorations[0].LocalPosition -= new Vector3(0, 0.3f, 0);
        decorations[1].LocalPosition -= new Vector3(0, 0.3f, 0);

        decorations[0].DecorationObject.GetComponentInChildren<MeshRenderer>().material = WorldRendererAccess.Get(Parent).MaterialsSource.SolidColor(SwitchColor);

        // not optimal but it was causing problems... (genuinely so confused, if you think you can fix this, dm me!)
        decorations[0].DecorationObject.GetComponentInChildren<BoxCollider>().RemoveComponentImmediate<BoxCollider>();

        if (StartOn)
            decorations[0].LocalRotation = Quaternion.Euler(40f, 0f, 0f);
        return decorations;
    }
    private static readonly RenderedEntity DummyEntity = typeof(RenderedEntity).Constructor().Invoke(null) as RenderedEntity;
    private static readonly Func<object, object[], object> RunGenerateDecorations = typeof(Switch).Method("GenerateDecorations").Invoke;
    private static readonly Accessor<ComponentDataManager, bool> WorldPositionRotationCalculatedAccess = new("WorldPositionRotationCalculated");
    private static readonly Accessor<ComponentDataManager, Quaternion> WorldRotationAccess = new("_WorldRotation");
    private static readonly Accessor<Switch, MeshRenderer> VisualSwitchAccess = new("VisualSwitch");

}

public class SwitchAddonGenerator : ClientAddonGenerator<ISwitchData>
{
    public override ClientAddon GenerateAddon(ComponentData componentData, ISwitchData data) => new SwitchAddon(data.Color, data.On);
    public override int GetBlockCount(ComponentData componentData) => 0;
    public override Block[] GenerateBlocks(ComponentData componentData, ISwitchData data) => [];
}
