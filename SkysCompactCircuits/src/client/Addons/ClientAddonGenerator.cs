using System;
using LogicAPI.Data;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Components;

namespace SkysCompactCircuits.Client.Addons;

public abstract class ClientAddonGenerator
{
    public abstract ClientAddon GenerateAddon(ComponentData componentData);
    public abstract Block[] GenerateBlocks(ComponentData componentData);
    // Can be overridden to save time but *must* produce the same result!
    public virtual int GetBlockCount(ComponentData componentData) => GenerateBlocks(componentData).Length;
}

public abstract class ClientAddonGenerator<TCustomData> : ClientAddonGenerator where TCustomData : class
{
    private readonly CustomDataManager<TCustomData> DataManager = new();
    public override Block[] GenerateBlocks(ComponentData componentData)
    {
        if (componentData.CustomData == null)
            return GenerateBlocks(componentData, null);
        if (!DataManager.TryDeserializeData(componentData.CustomData))
            throw new Exception($"Error deserializing data for component of type {componentData.Type}");

        return GenerateBlocks(componentData, DataManager.Data);
    }
    public override ClientAddon GenerateAddon(ComponentData componentData)
    {
        if (componentData.CustomData == null)
            return GenerateAddon(componentData, null);
        if (!DataManager.TryDeserializeData(componentData.CustomData))
            throw new Exception($"Error deserializing data for component of type {componentData.Type}");

        return GenerateAddon(componentData, DataManager.Data);
    }

    public abstract Block[] GenerateBlocks(ComponentData componentData, TCustomData data);
    public abstract ClientAddon GenerateAddon(ComponentData componentData, TCustomData data);
}
