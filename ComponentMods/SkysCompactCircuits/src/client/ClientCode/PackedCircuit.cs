using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LogicAPI.Data;
using LogicAPI.Data.BuildingRequests;
using LogicAPI.Interfaces;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.BinaryStuff;
using SkysCompactCircuits.Client.Addons;
using SkysCompactCircuits.Shared;
using SkysCompactCircuits.Shared.Packets;
using SkysGeneralLib.Client.BuildRequests;
using SkysGeneralLib.Client.TypeExtensions;
using UnityEngine;

namespace SkysCompactCircuits.Client.ClientCode;

public class PackedCircuit : ComponentClientCode
{
    private bool Setup = false;
    public IPackedCircuitData Data { get; protected set; }
    public readonly List<ClientAddon> Addons = [];

    protected override void InitializeInWorld()
    {
        // Because *somebody* is going to do it, and it is going to make a *mess* of world files!!!
        // (If you *really* want to, nothing is stopping you from making a zero size block...)
        // ((No, I have not tested this nor do I plan to))
        if (Decorations.Count + BlockCount + InputCount + OutputCount == 0 && Data is not null)
            new BuildRequest_RemoveComponentsAndChildrenAndAttachedWires([Address]).SendNoUndo();
    }

    public void RunSetup(Dictionary<ComponentAddress, ComponentAddress> addonMap)
    {
        if (Setup || Data is null)
            return;
        Setup = true;
        // At this point all children should already exist
        var consumedBlocks = Data.ComponentPrefab.Blocks.Length;
        foreach (var (addr, count, addon) in ClientAddonManager.GeneratorsFor(Data.PartialWorld, Data.AddonAddresses).Zip(Addons, (a, b) => (a.Address, a.Generator.GetBlockCount(a.Data), b)))
        {
            addon.SetReference(addonMap[addr], [.. Enumerable.Range(consumedBlocks, count).Select(GetBlockEntity)]);
            consumedBlocks += count;
        }
    }

    // We cannot just use the OnComponentMoved server hook because canceling a grab will also require a re-setup
    // (Hey, maybe that should be changed? I don't see why we couldn't just hide the decorations and stop updating the client code...)
    // ((Dont see why we couldn't do that for successful grabs too. Just add an on move hook here too...))
    protected override void OnComponentReRendered()
    {
        if (PlacedInMainWorld && Data is not null)
            Instances.SendData.Send(new RequestInitializationPacket() { componentToInitialize = Address });
    }

    protected override void OnComponentDestroyed()
    {
        if (Setup && Data is not null)
            Addons.Do(a => a.OnComponentDestroyed());
    }

    protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        if (Data is null)
            return [];
        var allDecorations = new List<IDecoration>();
        foreach (var (generator, position, rotation) in ClientAddonManager.TransformsAndGeneratorsFor(Data.PartialWorld, Data.AddonAddresses))
        {
            var addon = generator.GenerateAddon();
            addon.Parent = this;
            var decorations = addon.GenerateDecorations(parentToCreateDecorationsUnder);
            foreach (IDecoration decoration in decorations)
            {
                decoration.LocalPosition = (Data.TransformOffset + position * Data.TransformScale) * 0.3f + rotation * decoration.LocalPosition * Data.TransformScale;
                decoration.LocalRotation = rotation * decoration.LocalRotation;
                decoration.DecorationObject.transform.localScale = decoration.DecorationObject.transform.localScale * Data.TransformScale;
            }
            allDecorations.AddRange(decorations);
            Addons.Add(addon);
        }
        return [.. allDecorations];
    }

    public override byte[] SerializeCustomData() => Data.Encode();
    protected override void DeserializeData(byte[] data) => Data = PackedCircuitManager.TryDecode(data, WhenIndexBecomesAvailable);

    public void WhenIndexBecomesAvailable()
    {
        if (!PlacedInMainWorld)
            return;
        Instances.MainWorld.Renderer.EntityManager.ReRenderComponentAndAttachedWires(Address);
    }
}


public class InitializationActionHandler : IComponentActionMutationHandler
{
    public void HandleComponentAction(ComponentAddress componentAddress, IComponentInWorld componentInWorld, byte[] actionData)
    {
        using MemoryByteReader reader = new(actionData);
        var type = reader.ReadByte();
        if (type == 1)
        {
            var size = reader.ReadInt32();
            var addonMap = new Dictionary<ComponentAddress, ComponentAddress>(size);
            for (int i = 0; i < size; i++)
                addonMap[reader.ReadComponentAddress()] = reader.ReadComponentAddress();
            (componentAddress.GetClientCode() as PackedCircuit)?.RunSetup(addonMap);
        }
    }
}
