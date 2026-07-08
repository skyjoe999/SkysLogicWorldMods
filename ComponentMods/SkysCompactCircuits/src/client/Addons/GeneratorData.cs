using LogicAPI.Data;
using LogicWorld.SharedCode.Components;

namespace SkysCompactCircuits.Client.Addons;

public record struct GeneratorData(ComponentAddress Address, ComponentData Data, ClientAddonGenerator Generator)
{
    public static implicit operator (ComponentAddress address, ComponentData data, ClientAddonGenerator generator)(GeneratorData value) => (value.Address, value.Data, value.Generator);
    public static implicit operator GeneratorData((ComponentAddress address, ComponentData data, ClientAddonGenerator generator) value) => new(value.address, value.data, value.generator);

    public readonly Block[] GenerateBlocks() => Generator.GenerateBlocks(Data);
    public readonly ClientAddon GenerateAddon() => Generator.GenerateAddon(Data);
}