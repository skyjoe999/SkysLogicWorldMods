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

    protected override byte[] SerializeCustomData() => Data.Encode();
    protected override void DeserializeData(byte[] data)
    {
        try { Data = PackedCircuitManager.DecodeAndIndex(data); }
        catch (KeyNotFoundException)
        {
            // Figure out what to do here?
            // I think I'm willing to just hope this never happens...
            Logger.Error($"Couldn't find key for data {Convert.ToHexString(data)}");
        }
    }

    protected (ComponentAddress, ComponentAddress)[] AddonMap;
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
        var orderedChildren = GetAllChildren(Address).ToArray();


        // Connect up the export pegs
        foreach (var (exportIndex, packedIndex) in structure?.ExportIndices.Select((m, i) => (m, i)) ?? [])
            Inputs[packedIndex].AddSecretLinkWith(Services.ICircuitryManager.LookupInput(new(orderedChildren[exportIndex], 0)));

        // Link old and new addresses
        var addonAddresses = Data.AddonAddresses.ToHashSet();
        AddonMap = [.. structure?.OriginalChildAddresses.Select((a, i) => (address: a, childIndex: i))
            .Where(p => addonAddresses.Contains(p.address))
            .Select(p => (p.address, orderedChildren[p.childIndex]))
            ?? []];

        RunSetupOnClient();

        static IEnumerable<ComponentAddress> GetAllChildren(ComponentAddress root)
        {
            var queue = new Queue<ComponentAddress>();
            queue.Enqueue(root);
            while (queue.TryDequeue(out var next))
            {
                yield return next;
                foreach (var child in next.GetComponent().EnumerateChildren())
                    queue.Enqueue(child);
            }
        }
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
}
