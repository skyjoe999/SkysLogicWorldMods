using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using JECS;
using LICC;
using LogicAPI.Data;
using LogicAPI.Server;
using LogicAPI.Server.Components;
using LogicLog;
using LogicWorld.Server.Circuitry;
using LogicWorld.Server.Saving;
using LogicWorld.SharedCode.Data;
using SkysCompactCircuits.Shared;
using SkysCompactCircuits.Shared.Packets;
using SkysGeneralLib.Server;
using SkysGeneralLib.Shared.AccessTools;
using SkysGeneralLib.Shared.Networking;
using SkysGeneralLib.Shared.TypeExtensions;

namespace SkysCompactCircuits.Server;

[HarmonyPatch]
public class SkysCompactCircuits_ServerMod : ServerMod
{
    protected override void Initialize()
    {
        new Harmony(Manifest.ID).PatchAll();

        RegisterKnownExcludedTypes();

        PackedCircuitManager.OnIndexAdded += (index, data) =>
        {
            // In theory maybe we could do something fancy by only appending but for now this is fine.
            PackedCircuitFileManager.AppendNewIndex(index, data);
            Services.NetworkServer.Broadcast(new NewCircuitRegisteredPacket() { index = index, newCircuitData = data });
        };

        FuncPacketHandler<IndexCircuitRequestPacket>.Add((packet, connection, _) =>
        {
            byte[] result = [];
            try
            {
                if (packet.rawCircuitData is null || packet.rawCircuitData.Length == 0)
                    throw new($"Packet data {(packet.rawCircuitData is null ? "null" : "empty")}");
                IPackedCircuitData.AcceptModes((IPackedCircuitData.Mode)packet.rawCircuitData[0], IPackedCircuitData.Mode.Full, IPackedCircuitData.Mode.Compressed);

                var rawCircuit = PackedCircuitManager.Decode(packet.rawCircuitData);
                ModifiedIndexResolver.ConsolidateModified(ref rawCircuit);

                result = PackedCircuitManager.DecodeAndIndex(rawCircuit).Encode();
            }
            catch (Exception exception) { Logger.Exception(exception, $"Encounter exception while processing {nameof(IndexCircuitRequestPacket)}"); }

            Services.NetworkServer.Send(connection, new IndexCircuitResponsePacket() { indexCircuitData = result });
        });
    }

    public static void RegisterKnownExcludedTypes()
    {
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Peg");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.ThroughPeg");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Chair");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Flag");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Label");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.PanelLabel");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Mount");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.CircuitBoard");

        PackedCircuitStructureManager.RegisterExcludedType("SkysCompactCircuits.PackedCircuit"); // if it's empty
        PackedCircuitStructureManager.RegisterExcludedType("SkysCompactCircuits.SimulationStorage"); // not implemented

        // Known modded components
        PackedCircuitStructureManager.RegisterExcludedType("HoverPads.HoverPad");
        PackedCircuitStructureManager.RegisterExcludedType("SkysWirelessBus.WirelessBus");
        PackedCircuitStructureManager.RegisterExcludedType("BoardPegs.BoardPeg");
        PackedCircuitStructureManager.RegisterExcludedType("BoardPegs.BoardPegWalled");
        PackedCircuitStructureManager.RegisterExcludedType("MorePegs.BoardPeg");
        PackedCircuitStructureManager.RegisterExcludedType("LWGlass.Glass");
        PackedCircuitStructureManager.RegisterExcludedType("EcconiaCPUServerComponents.FlatKey");

        // Sockets </3 (Not sure if I can do anything with these yet...)
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Socket");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.ChubbySocket");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.ThroughSocket");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.ChubbyThroughSocket");
        PackedCircuitStructureManager.RegisterExcludedType("LabelSockets.LabelSocket");
        PackedCircuitStructureManager.RegisterExcludedType("LabelSockets.ChubbyLabelSocket");

        // These should only be included on the root level
        PackedCircuitStructureManager.RegisterExcludedType("SkysCompactCircuits.ExportPeg");
        PackedCircuitStructureManager.RegisterExcludedType("SkysCompactCircuits.ExportThroughPeg");

        // All addons should be skipped if not acting as an addon
        PackedCircuitStructureManager.RegisterExcludedType("MHG.StandingDisplay");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.PanelDisplay");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Button");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.PanelButton");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Switch");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.PanelSwitch");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.PanelSwitch");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.PanelKey");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Key");
    }


    private static readonly ILogicLogger CullLogger = LogicLogger.For("SkysCompactCircuits.Culling");

    // This will break things if components are currently being cloned / are in the undo history / etc.
    [HarmonyPatch(typeof(SaveManager), "ReloadActiveSave")]
    [HarmonyPostfix]
    [Command("CompactCircuits.CullIndexedData")]
    public static void CullIndexedData()
    {
        CullLogger.Trace($"Trying Culling starting with {PackedCircuitManager.CircuitDataByIndex.Count} circuits");

        var components = Services.ICircuitryManager is CircuitryManager manager
            ? new Accessor<CircuitryManager, Dictionary<ComponentAddress, LogicComponent>>("LogicComponents").Get(manager).Values
            : Services.IWorldData.AllComponents.Select(p => Services.ICircuitryManager.LookupComponent(p.Key)); // slower but technically correct

        var usedIndices = new HashSet<int>();
        var extraIndices = new Queue<int>();
        foreach (var component in components)
            if (component is PackedCircuit circuit && circuit.Data is not null)
                usedIndices.Add(circuit.Data.Index);
        foreach (var item in GetAllHotbarDatas().SelectMany(hotbar => hotbar?.HotbarItems ?? []))
            if (item is DetailedHotbarItemData detailed && detailed.TextID == "SkysCompactCircuits.PackedCircuit")
            {
                var circuit = PackedCircuitManager.Decode(detailed.CustomData);
                if (circuit is null)
                    continue; // this means the index was not found, this should never happen, but if it does theres no sense worrying here
                if (circuit is IndexedPackedCircuitData indexed)
                    if (!usedIndices.Add(indexed.Index))
                        continue; // if we already found it all its children should be loaded too

                ProcessInner(circuit);
            }

        // We cannot assume an inner world will contain all its inner-inner dependencies.
        while (extraIndices.TryDequeue(out var index))
            if (usedIndices.Add(index))
                ProcessInner(PackedCircuitManager.LookupIndexed(index));


        if (PackedCircuitManager.CircuitDataByIndex.Count == usedIndices.Count)
            return; // Yay! Nothing to cull! ^^

        CullLogger.Info("Culling circuits with ids " + PackedCircuitManager.CircuitDataByIndex.Keys.Except(usedIndices).Select(i => i + "").Aggregate());
        foreach (var index in PackedCircuitManager.CircuitDataByIndex.Keys.Except(usedIndices))
        {
            PackedCircuitManager.CircuitDataByIndex.Remove(index);
            Services.NetworkServer.Broadcast(new RemoveIndexedCircuitTrackingPacket() { indexToRemove = index });
        }
        PackedCircuitFileManager.WriteToDisk();

        // but now the hash lookups are wrong, easiest solution is to just reload everything
        PackedCircuitFileManager.ReadFromDisk();
        // (these are also out of date (if they somehow exist at all))
        PackedCircuitStructureManager.StructuresByIndex.Clear();
        PackedCircuitStructureManager.ExtraWorldsByGuid.Clear();

        // we need to find all the inner circuits too in case they aren't placed in the world
        void ProcessInner(IPackedCircuitData circuit)
        {
            if (circuit?.PartialWorld.ComponentIDsMap.Where(p => p.Value == "SkysCompactCircuits.PackedCircuit").Aggregate((ushort?)null, (_, v) => v.Key) is ushort CircuitID)
                foreach (var (_, componentData) in circuit.PartialWorld.OrderedComponentsAndAddresses)
                    if (componentData.Type.NumericID == CircuitID && PackedCircuitManager.TryGetIndex(componentData.CustomData, out var index))
                        extraIndices.Enqueue(index);
        }
    }

    [HarmonyPatch(typeof(SaveManager), "ReloadActiveSave")]
    [HarmonyPrefix]
    public static void OnSaveLoad()
    {
        PackedCircuitFileManager.TryConvertLegacyFiles();
        PackedCircuitFileManager.ReadFromDisk();
    }

    public static IEnumerable<HotbarData> GetAllHotbarDatas()
    {
        var dir = new DirectoryInfo(Path.Combine(Services.ISaveManager.ActiveSaveDirectory, "players"));
        return !dir.Exists ? [] : dir
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .Select(file => new WrappedObjectDataFile<SavePlayerValues>(file.FullName, null).Data.Hotbar);
    }
}
