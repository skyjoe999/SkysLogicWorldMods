using System;
using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using UnityEngine;

namespace SkysCompactCircuits.Client.Addons;



public static class ClientAddonManager
{
    private static readonly Dictionary<string, ClientAddonGenerator> Generators = [];
    public static void RegisterAddonType(string textID, ClientAddonGenerator generator) => Generators[textID] = generator;

    public static Dictionary<ComponentType, ClientAddonGenerator> GetRelevantGenerators(IReadOnlyDictionary<ushort, string> componentIDsMap) =>
        componentIDsMap?.Where(p => Generators.ContainsKey(p.Value)).ToDictionary(p => new ComponentType(p.Key), p => Generators[p.Value])
        ?? throw new ArgumentNullException(nameof(componentIDsMap));

    #region Data parsing
    public static IEnumerable<(ComponentAddress address, ComponentData data, Vector3 position, Quaternion rotation)> TransformsFor(PartialWorldData world)
    {
        // NOOOOO why is this breadth first! We could have used a simple stack algorithm TT
        if (world is null) throw new ArgumentNullException(nameof(world));

        var transforms = new Dictionary<ComponentAddress, (Vector3 position, Quaternion rotation)>() { [ComponentAddress.Empty] = (Vector3.zero, Quaternion.identity) };
        foreach (var (address, data) in world.OrderedComponentsAndAddresses)
        {
            var (parentPosition, parentRotation) = transforms[data.Parent];
            var (position, rotation) = (parentPosition + parentRotation * data.LocalPosition / 0.3f, parentRotation * data.LocalRotation);
            transforms[address] = (position, rotation);
            yield return (address, data, position, rotation);
        }
    }
    public static IEnumerable<GeneratorData> GeneratorsFor(PartialWorldData world)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        var relevantGenerators = GetRelevantGenerators(world.ComponentIDsMap);

        foreach (var (address, componentData) in world.OrderedComponentsAndAddresses)
            if (relevantGenerators.TryGetValue(componentData.Type, out ClientAddonGenerator generator))
                yield return (address, componentData, generator);
    }
    public static IEnumerable<GeneratorData> GeneratorsFor(PartialWorldData world, IEnumerable<ComponentAddress> addons)
    {
        var relevantAddresses = addons?.ToHashSet() ?? throw new ArgumentNullException(nameof(addons));
        return GeneratorsFor(world).Where(g => relevantAddresses.Contains(g.Address));
    }

    public static IEnumerable<(GeneratorData generator, Vector3 position, Quaternion rotation)> TransformsAndGeneratorsFor(PartialWorldData world)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        var relevantGenerators = GetRelevantGenerators(world.ComponentIDsMap);

        foreach (var (address, componentData, position, rotation) in TransformsFor(world))
            if (relevantGenerators.TryGetValue(componentData.Type, out ClientAddonGenerator generator))
                yield return ((address, componentData, generator), position, rotation);
    }
    public static IEnumerable<(GeneratorData generator, Vector3 position, Quaternion rotation)> TransformsAndGeneratorsFor(PartialWorldData world, IEnumerable<ComponentAddress> addons)
    {
        var relevantAddresses = addons?.ToHashSet() ?? throw new ArgumentNullException(nameof(addons));
        return TransformsAndGeneratorsFor(world).Where(g => relevantAddresses.Contains(g.generator.Address));
    }
    #endregion

    static ClientAddonManager()
    {
        RegisterAddonType("MHG.StandingDisplay", new StandingDisplayAddonGenerator());
        RegisterAddonType("MHG.PanelDisplay", new PanelDisplayAddonGenerator());
        RegisterAddonType("MHG.Button", new ButtonAddonGenerator());
        RegisterAddonType("MHG.PanelButton", new PanelButtonAddonGenerator());
        RegisterAddonType("MHG.Switch", new SwitchAddonGenerator());
        RegisterAddonType("MHG.PanelSwitch", new SwitchAddonGenerator());
    }
}
