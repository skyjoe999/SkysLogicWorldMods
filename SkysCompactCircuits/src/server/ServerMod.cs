using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using JECS;
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

        FuncPacketHandler<IndexCircuitRequestPacket>.Add((packet, connection, _) =>
            Services.NetworkServer.Send(connection, new IndexCircuitResponsePacket() { data = PackedCircuitManager.DecodeAndIndex(packet.data).Encode() })
        );
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
        // Known modded components
        PackedCircuitStructureManager.RegisterExcludedType("HoverPads.HoverPad");
        PackedCircuitStructureManager.RegisterExcludedType("SkysWirelessBus.WirelessBus");
        PackedCircuitStructureManager.RegisterExcludedType("BoardPegs.BoardPeg");
        PackedCircuitStructureManager.RegisterExcludedType("BoardPegs.BoardPegWalled");
        // Sockets </3 (Not sure if I can do anything with these yet...)
        PackedCircuitStructureManager.RegisterExcludedType("MHG.Socket");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.ChubbySocket");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.ThroughSocket");
        PackedCircuitStructureManager.RegisterExcludedType("MHG.ChubbyThroughSocket");
        PackedCircuitStructureManager.RegisterExcludedType("LabelSockets.LabelSocket");
        PackedCircuitStructureManager.RegisterExcludedType("LabelSockets.ChubbyLabelSocket");
    }

    private static readonly ILogicLogger CullLogger = LogicLogger.For("SkysCompactCircuits.Culling");

    // This will break things if components are currently being cloned / are in the undo history / etc.
    [HarmonyPatch(typeof(SaveManager), "ReloadActiveSave")]
    [HarmonyPostfix]
    public static void CullIndexedData()
    {
        if (!PackedCircuitManager.ExtraDataManager.ExtraData.HasData)
        {
            PackedCircuitManager.ExtraDataManager.ExtraData.RunAsSoonAsDataAvailable(_ => CullIndexedData());
            return;
        }

        CullLogger.Trace($"Trying Culling starting with {PackedCircuitManager.CircuitDataByIndex.Count} circuits");

        var components = Services.ICircuitryManager is CircuitryManager manager
            ? new Accessor<CircuitryManager, Dictionary<ComponentAddress, LogicComponent>>("LogicComponents").Get(manager).Values
            : Services.IWorldData.AllComponents.Select(p => Services.ICircuitryManager.LookupComponent(p.Key)); // slower but technically correct

        var UsedIndices = new HashSet<int>();
        foreach (var component in components)
            if (component is PackedCircuit circuit && circuit.Data is not null)
                UsedIndices.Add(circuit.Data.Index);
        foreach (var item in GetAllHotbarDatas().SelectMany(hotbar => hotbar?.HotbarItems ?? []))
            if (item is DetailedHotbarItemData detailed && detailed.TextID == "SkysCompactCircuits.PackedCircuit")
            {
                var circuit = PackedCircuitManager.TryDecode(detailed.CustomData);
                if (circuit is null)
                    continue; // this means the index was not found, this should never happen, but if it does theres no sense worrying here
                if (circuit is IndexedPackedCircuitData indexed)
                    if (!UsedIndices.Add(indexed.Index))
                        continue; // if we already found it all its children should be loaded too

                // we need to find all the inner circuits too in case they aren't placed in the world
                if (circuit.PartialWorld.ComponentIDsMap.Where(p => p.Value == "SkysCompactCircuits.PackedCircuit").Aggregate((ushort?)null, (_, v) => v.Key) is ushort CircuitID)
                    foreach (var (_, componentData) in circuit.PartialWorld.OrderedComponentsAndAddresses)
                        if (componentData.Type.NumericID == CircuitID && PackedCircuitManager.TryDecode(componentData.CustomData) is IndexedPackedCircuitData innerIndexed)
                            UsedIndices.Add(innerIndexed.Index);
            }

        if (PackedCircuitManager.CircuitDataByIndex.Count == UsedIndices.Count)
            return; // Yay! Nothing to cull! ^^

        CullLogger.Trace("Culling circuits with ids " + PackedCircuitManager.CircuitDataByIndex.Keys.Except(UsedIndices).Select(i => i + "").Aggregate());
        PackedCircuitManager.CircuitDataByIndex.Keys.Except(UsedIndices).Do(i => PackedCircuitManager.CircuitDataByIndex.Remove(i));
        PackedCircuitManager.ExtraDataManager.WriteToExtraData(checkSize: false);

        // but now the hash lookups are wrong, easiest solution is to just reload everything
        PackedCircuitManager.ExtraDataManager.ReadFromExtraData();
        // (these are also out of date (if they somehow exist at all))
        PackedCircuitStructureManager.StructuresByIndex.Clear();
        PackedCircuitStructureManager.AdditionWorldsByGuid.Clear();
    }

    [HarmonyPatch(typeof(SaveManager), "ReloadActiveSave")]
    [HarmonyPrefix]
    public static void SetupExtraData() => PackedCircuitManager.ExtraDataManager.SetupExtraData(Services.ExtraData);
    public static IEnumerable<HotbarData> GetAllHotbarDatas()
    {
        var dir = new DirectoryInfo(Path.Combine(Services.ISaveManager.ActiveSaveDirectory, "players"));
        return !dir.Exists ? [] : dir
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .Select(file => new WrappedObjectDataFile<SavePlayerValues>(file.FullName, null).Data.Hotbar);
    }
}
