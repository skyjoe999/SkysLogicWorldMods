using System;
using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicAPI.Server.Components;
using LogicAPI.WorldDataMutations;
using LogicWorld.SharedCode.BinaryStuff;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Server;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysCompactCircuits.Server;

public class PackedCircuit : LogicComponent
{
    public bool Setup = false;
    public bool IsChildOfCircuit = false;
    public IndexedPackedCircuitData Data { get; protected set; }

    protected override byte[] SerializeCustomData() => Data?.Encode();
    protected override void DeserializeData(byte[] data)
    {
        if (data is null)
            Logger.Error($"Custom data is null for component {Address}");
        if (PackedCircuitManager.TryGetIndex(data, out var index) && !PackedCircuitManager.IsIndexValid(index))
            Logger.Error($"Couldn't find key for data {index}");
        else
            Data = PackedCircuitManager.DecodeAndIndex(data);

        if (Data is not null && !Data.Encode().SequenceEqual(data))
            Services.IWorldMutationManager.ForceDataRefresh(this);
    }

    public (ComponentAddress original, ComponentAddress child)[] AddonMap;
    public Dictionary<ComponentAddress, ComponentAddress> ChildMap;
    public int ClaimedExports = 0;
    protected override void Initialize()
    {
        var packedCircuitType = Component.Data.Type;
        var parent = Component.Data.Parent;
        var component = parent.GetComponent();
        while (parent.IsNotEmpty() && component.Data.Type != packedCircuitType)
        {
            parent = component.Parent;
            component = parent.GetComponent();
        }
        if (!parent.IsEmpty())
            IsChildOfCircuit = true;
        else
            SetupHooks.EnsureSetup(this);
    }

    public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex) => false;

    public void EnsureSetupAndSendToClient()
    {
        if (Setup)
            RunSetupOnClient(); // we dont want to run this twice!
        else
            RunSetup();
    }

    public void RunSetup()
    {
        if (Setup || IsChildOfCircuit || Data is null)
            return;
        Setup = true;

        var structure = PackedCircuitStructureManager.GenerateStructureWithCache(Data);

        ChildMap = structure is not null ? GenerateChildMap(this, structure.Value) : [];

        // Connect up the export pegs
        var exportPegs = structure?.ExportAddresses.Where(address => address.IsInputAddress())
            .Select((address, i) => (i, new InputAddress(ChildMap.GetValueOrDefault(address.ComponentAddress), address.PegIndex)))
            .Take(Inputs.Count) ?? [];
        foreach (var (packedIndex, exportPegAddress) in exportPegs)
            Services.ICircuitryManager.LookupInput(exportPegAddress)?.AddSecretLinkWith(Inputs[packedIndex]);

        // Link old and new addresses
        AddonMap = [.. Data.AddonAddresses.Select(original => (original, ChildMap.GetValueOrDefault(original)))];

        RunSetupOnClient();
    }

    protected void RunSetupOnClient()
    {
        if (!Setup || Data is null)
            return;
        // since we know exactly what data we're sending we can calculate the buffer size in advance
        var writer = new ByteWriter(AddonMap.Length * 8 + 4 + 1);

        writer.Write((byte)1); // identifier byte (always good to leave room for expansion <3)
        writer.Write(AddonMap.Length);

        foreach (var (saved, world) in AddonMap)
            writer.Write(saved).Write(world);
        Services.IWorldUpdates.QueueMutationToBeSentToClient(new WorldMutation_SendComponentAction()
        {
            AddressOfTargetComponent = Address,
            ActionData = writer.Finish(),
        });
    }

    public IOutputPeg GetNextExportOutput() => Outputs.ElementAtOrDefault(ClaimedExports++);


    public static Dictionary<ComponentAddress, ComponentAddress> GenerateChildMap(PackedCircuit circuit, PackedCircuitStructure structure)
    {
        /*
        The goal here is to match up the children that exist to the children that are in the partial world data.
        There are a few things that can happen to make this non-trivial:
            1. Text IDs have been added to the ignore list.
            2. Text IDs have been removed from the ignore list.
            3. Components with children are kept regardless of the ignore list.
            4. Components with children may not always be included in the future.
            5. Mods doing unexpected things. (Not much can be done about this one by definition.)
        Assumptions we can rely on:
            - Components that are matchable are in the correct order.
            - Components will never change type.
            - Components will never swap parents (but may be re-parented to the grandparent).
        I shall ignore these assumptions and implement it however I can manage. </3
        */

        var map = new Dictionary<ComponentAddress, ComponentAddress>();
        var originalParentage = structure.AdditionWorld.OrderedComponentsAndAddresses
            .Select((p, i) => (index: i, p.address, type: p.componentData.Type, parent: p.componentData.Parent))
            .GroupBy(p => p.parent)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(p => p.index)
                .GroupBy(p => p.type, p => p)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.index).Select(p => p.address))
            );

        var queue = new Queue<(IEnumerable<ComponentAddress> component, Dictionary<ComponentType, IEnumerable<ComponentAddress>> originalChildren)>();
        queue.Enqueue((circuit.Component.EnumerateChildren(), originalParentage.GetValueOrDefault(structure.AdditionWorld.OrderedComponentsAndAddresses[0].address)));

        while (queue.TryDequeue(out var next))
        {
            var byType = next.component
                .Select((a, i) => (index: i, address: a, data: a.GetComponent()))
                .GroupBy(p => p.data.Data.Type);
            foreach (var group in byType)
            {
                foreach (var (actual, original) in group.OrderBy(g => g.index).Zip(next.originalChildren?.GetValueOrDefault(group.Key) ?? []))
                {
                    map[original] = actual.address;
                    queue.Enqueue((actual.data.EnumerateChildren(), originalParentage.GetValueOrDefault(original)));
                }
            }
        }
        return map;
    }
}
