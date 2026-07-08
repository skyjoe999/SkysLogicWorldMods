using System;
using System.Linq;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysCompactCircuits.Client.Addons;

public class DisplayAddon<DisplayType, TData> : SuperAddon<DisplayType> where DisplayType : GenericDisplay<TData>, new() where TData : class, IDisplayData
{
    protected override void Initialize()
    {
        base.Initialize();
        BlockEntitiesAccess.Set(Inner, BlockEntities);
        InputEntitiesAccess.Set(Inner, new IRenderedEntity[ReferenceComponent.Data.InputCount]);
        WorldRendererAccess.Get(Parent).DisplayConfigurations.RunOnConfigurationOrder(Inner.InputCount, order =>
        {
            if (!order.Contains(Inner.Data.DisplayConfigurationIndex))
                Inner.Data.DisplayConfigurationIndex = order.FirstOrDefault();
        });
        BlockEntities[0].SetColor(new(1, 0, 0));
        Inner.QueueFrameUpdate();
    }
    public override void OnComponentDestroyed()
    {
        base.OnComponentDestroyed();
        var WorldRenderer = WorldRendererAccess.Get(Parent);
        WorldRenderer.DisplayConfigurations.StopReceivingConfigurationUpdates(new DisplayConfigurationAddress(Inner.InputCount, Inner.Data.DisplayConfigurationIndex), Inner);
        WorldRenderer.AllDisplays.StopTracking(new DisplayConfigurationAddress(Inner.InputCount, Inner.Data.DisplayConfigurationIndex), Inner.Data);
    }

}

public class StandingDisplayAddonGenerator : ClientAddonGenerator<IDisplayData>
{
    public override ClientAddon GenerateAddon(ComponentData componentData, IDisplayData data) => new DisplayAddon<StandingDisplay, IDisplayData>();
    public override int GetBlockCount(ComponentData componentData) => 1;

    public override Block[] GenerateBlocks(ComponentData componentData, IDisplayData data) => [new()
    {
        RawColor = GetColor(componentData.InputCount, data.DisplayConfigurationIndex),
        Scale = componentData.InputCount <= 3 ? Vector3.one : componentData.InputCount == 4 ? new(2, 1, 2) : new(componentData.InputCount, 1, 1),
        Position = componentData.InputCount <= 3 ? default : new Vector3(-0.5f, 0, -0.5f),
        MeshName = componentData.InputCount <= 3 ? "BetterCube" : "OriginCube",
    }];
    public static Color24 GetColor(int inputCount, int index)
    {
        Instances.MainWorld.Renderer.DisplayConfigurations.RunOnConfigurationOrder(inputCount, order =>
        {
            if (!order.Contains(index))
                index = order.FirstOrDefault();
        });

        Color24? result = null;
        Instances.MainWorld.Renderer.DisplayConfigurations.RunOnConfiguration(
            new DisplayConfigurationAddress(inputCount, index),
            colors => result = colors?.FirstOrDefault()
        );
        return result ?? new(0x202020);
    }
}

public class PanelDisplayAddonGenerator : ClientAddonGenerator<IPanelDisplayData>
{
    public override ClientAddon GenerateAddon(ComponentData componentData, IPanelDisplayData data) => new DisplayAddon<SafeDisplay, IPanelDisplayData>();
    public override int GetBlockCount(ComponentData componentData) => 1;

    public override Block[] GenerateBlocks(ComponentData componentData, IPanelDisplayData data) => [new()
    {
        RawColor = StandingDisplayAddonGenerator.GetColor(componentData.InputCount, data.DisplayConfigurationIndex),
        Scale = new(data.SizeX, 1, data.SizeZ),
        Position = new Vector3(data.SizeX - 1, 0, data.SizeZ - 1) / 2f,
    }];
    public class SafeDisplay : PanelDisplay { protected override void DataUpdate() { } }
}
