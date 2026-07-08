using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LogicAPI.Data;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Chunks;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.Components;
using SkysGeneralLib.Client.TypeExtensions;
using SkysGeneralLib.Shared.AccessTools;
using UnityEngine;

namespace SkysCompactCircuits.Client.Addons;

// Just contains useful accessors/reverse patches
// Nothing about this is strictly necessary if you want to implement your own addon
[HarmonyPatch]
public class SuperAddon : ClientAddon
{
    [HarmonyPatch(typeof(ComponentClientCode), "SetupForWorld_BeforePrefab")]
    [HarmonyReversePatch] public static void SetupForWorld_BeforePrefab(ComponentClientCode instance, object worldRenderer, ComponentAddress cAddress, ComponentInfo info, ComponentDataManager componentDataManager) => throw new NotImplementedException("It's a stub");
    [HarmonyPatch("CircuitStatesManager", "AddClientCode")]
    [HarmonyReversePatch] public static void AddClientCode(object CircuitStatesManager, ComponentClientCode code, int index) => throw new NotImplementedException("It's a stub");
    [HarmonyPatch("EntityTracker", "RegisterNewPeg")]
    [HarmonyReversePatch] public static void RegisterNewPeg(object EntityTracker, PegAddress pAddress, RenderedEntity pegEntity, Vector3 wirePoint) => throw new NotImplementedException("It's a stub");

    public static readonly Accessor<ComponentClientCode, IWorldRenderer> WorldRendererAccess = new("WorldRenderer");
    public static readonly Accessor<ComponentClientCode, IReadOnlyList<IRenderedEntity>> BlockEntitiesAccess = new("BlockEntities");
    public static readonly Accessor<ComponentClientCode, IReadOnlyList<IRenderedEntity>> InputEntitiesAccess = new("InputEntities");
    public static readonly Accessor<ComponentClientCode, IReadOnlyList<IRenderedEntity>> OutputEntitiesAccess = new("OutputEntities");
    public static readonly Accessor<ComponentClientCode, ComponentDataManager> ComponentDataManagerAccess = new("ComponentDataManager");
    public static readonly Accessor<ComponentClientCode, bool> PlacedInMainWorldAccess = new("PlacedInMainWorld");
    public static readonly Accessor<ComponentClientCode, IReadOnlyList<IDecoration>> DecorationsAccess = new("Decorations");
    public static readonly Accessor<ComponentClientCode, bool> HasBeenFullyInitializedAccess = new("HasBeenFullyInitialized");
    public static readonly Func<object[], object> NewComponentDataManager = typeof(ComponentDataManager).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke;
    public static readonly Func<object, object> GetComponentDataManagerWorldData = typeof(ComponentDataManager).GetProperty("WorldData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue;
    public static readonly Func<object, object[], object> CallDataUpdate = typeof(ComponentClientCode).Method("DataUpdate").Invoke;
    public static Accessor<EntityTracker, Dictionary<ComponentAddress, ComponentClientCode>> AllClientCodeAccess => new("_AllClientCode");
}

public class SuperAddon<InnerType> : SuperAddon where InnerType : ComponentClientCode, new()
{
    public IComponentInWorld ReferenceComponent;
    public InnerType Inner;

    protected override void Initialize()
    {
        ReferenceComponent = Reference.GetComponent();
        Inner ??= new();
        var referenceWorldData = GetComponentDataManagerWorldData(ComponentDataManagerAccess.Get(Parent));
        SetupForWorld_BeforePrefab(
            Inner,
            Instances.MainWorld.Renderer,
            Reference,
            Instances.MainWorld.ComponentTypes.GetComponentInfo(ReferenceComponent.Data.Type),
            NewComponentDataManager([ReferenceComponent.Data, Reference, referenceWorldData]) as ComponentDataManager
        );
        PlacedInMainWorldAccess.Set(Inner, true);

        AllClientCodeAccess.Get((EntityTracker)Instances.MainWorld.Renderer.Entities)[Reference] = Inner;

        foreach (var (info, i) in ReferenceComponent.Data.InputInfos.Select((v, i) => (v, i)))
        {
            // We need this to be tracked so the state change packets will change the state properly
            RegisterNewPeg(Instances.MainWorld.Renderer.Entities, new(Reference, i, PegType.Input), null, default);
            AddClientCode(Instances.MainWorld.CircuitStates, Inner, info.StateID);
        }

        foreach (var (info, i) in ReferenceComponent.Data.OutputInfos.Select((v, i) => (v, i)))
        {
            RegisterNewPeg(Instances.MainWorld.Renderer.Entities, new(Reference, i, PegType.Output), null, default);
            AddClientCode(Instances.MainWorld.CircuitStates, Inner, info.StateID);
        }
    }
}
