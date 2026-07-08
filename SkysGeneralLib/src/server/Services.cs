using System;
using EccsLogicWorldAPI.Server;
using LICC;
using LICC.API;
using LogicAPI.Networking;
using LogicAPI.Server.Configuration;
using LogicAPI.Server.Managers;
using LogicAPI.Server.Networking;
using LogicAPI.Services;
using LogicWorld.Server;
using LogicWorld.Server.Circuitry;
using LogicWorld.Server.HostServices;
using LogicWorld.Server.Managers;
using LogicWorld.Server.Networking;
using LogicWorld.Server.Saving;
using LogicWorld.SharedCode.Components;
using LogicWorld.SharedCode.ExtraData;

namespace SkysGeneralLib.Server;

public static class Services
{
    public static ComponentTypesManager ComponentTypesManager => _ComponentTypesManager.Value;
    public static ICircuitryManager ICircuitryManager => _ICircuitryManager.Value;
    public static IClusterFactory IClusterFactory => _IClusterFactory.Value;
    public static INetworkManager INetworkManager => _INetworkManager.Value;
    public static ISimulationManager ISimulationManager => _ISimulationManager.Value;
    public static IWorldData IWorldData => _IWorldData.Value;
    public static IWorldDataMutator IWorldDataMutator => _IWorldDataMutator.Value;
    public static IWorldMutationManager IWorldMutationManager => _IWorldMutationManager.Value;
    public static IWorldUpdates IWorldUpdates => _IWorldUpdates.Value;
    public static NetworkServer NetworkServer => _NetworkServer.Value;
    public static IModManager IModManager => _IModManager.Value;
    public static IExtraData_ServerManager IExtraData_ServerManager => _IExtraData_ServerManager.Value;
    public static ISenderShortcuts ISenderShortcuts => _ISenderShortcuts.Value;
    public static LaunchOptions LaunchOptions => _LaunchOptions.Value;
    public static ISaveManager ISaveManager => _ISaveManager.Value;
    public static IBackupManager IBackupManager => _IBackupManager.Value;
    public static PartialWorldTicker_TrimStalePartialWorlds PartialWorldTicker_TrimStalePartialWorlds => _PartialWorldTicker_TrimStalePartialWorlds.Value;
    public static IPlayerSaveManager IPlayerSaveManager => _IPlayerSaveManager.Value;
    public static IServer IServer => _IServer.Value;
    public static ExtraData ExtraData => _ExtraData.Value;
    public static IAutosaver IAutosaver => _IAutosaver.Value;
    public static PacketIndexMapSyncHelper PacketIndexMapSyncHelper => _PacketIndexMapSyncHelper.Value;
    public static IBuildingManager IBuildingManager => _IBuildingManager.Value;
    public static ComponentActionBuildRequestManager ComponentActionBuildRequestManager => _ComponentActionBuildRequestManager.Value;
    public static ILogicManager ILogicManager => _ILogicManager.Value;
    public static CircuitStates CircuitStates => _CircuitStates.Value;
    public static CommandConsole CommandConsole => _CommandConsole.Value;
    public static IComponentRestrictions IComponentRestrictions => _IComponentRestrictions.Value;
    public static ILocalNetworkAnnounceService ILocalNetworkAnnounceService => _ILocalNetworkAnnounceService.Value;
    public static Sender Sender => _Sender.Value;
    public static IHailHandler IHailHandler => _IHailHandler.Value;
    public static Frontend Frontend => _Frontend.Value;
    public static IServerPartialWorldsManager IServerPartialWorldsManager => _IServerPartialWorldsManager.Value;
    public static PartialWorldTicker_CullStalePartialWorldActions PartialWorldTicker_CullStalePartialWorldActions => _PartialWorldTicker_CullStalePartialWorldActions.Value;
    public static IGameConfig IGameConfig => _IGameConfig.Value;
    public static IExtraData_FileManager IExtraData_FileManager => _IExtraData_FileManager.Value;
    public static IReceiver IReceiver => _IReceiver.Value;
    public static IPlayerManager IPlayerManager => _IPlayerManager.Value;
    public static IChatManager IChatManager => _IChatManager.Value;
    public static Paths Paths => _Paths.Value;

    public static SelfUpdatingContainer<Cluster> ClusterContainer => _ClusterContainer.Value;
    public static SelfUpdatingContainer<ClusterLinker> ClusterLinkerContainer => _ClusterLinkerContainer.Value;

    // Depending on when this class is loaded, some of these might be null (I think?)
#region Lazy
    private static readonly Lazy<ComponentTypesManager> _ComponentTypesManager = new(ServiceGetter.getService<ComponentTypesManager>);
    private static readonly Lazy<ICircuitryManager> _ICircuitryManager = new(ServiceGetter.getService<ICircuitryManager>);
    private static readonly Lazy<IClusterFactory> _IClusterFactory = new(ServiceGetter.getService<IClusterFactory>);
    private static readonly Lazy<INetworkManager> _INetworkManager = new(ServiceGetter.getService<INetworkManager>);
    private static readonly Lazy<ISimulationManager> _ISimulationManager = new(ServiceGetter.getService<ISimulationManager>);
    private static readonly Lazy<IWorldData> _IWorldData = new(ServiceGetter.getService<IWorldData>);
    private static readonly Lazy<IWorldDataMutator> _IWorldDataMutator = new(ServiceGetter.getService<IWorldDataMutator>);
    private static readonly Lazy<IWorldMutationManager> _IWorldMutationManager = new(ServiceGetter.getService<IWorldMutationManager>);
    private static readonly Lazy<IWorldUpdates> _IWorldUpdates = new(ServiceGetter.getService<IWorldUpdates>);
    private static readonly Lazy<NetworkServer> _NetworkServer = new(ServiceGetter.getService<NetworkServer>);
    private static readonly Lazy<IModManager> _IModManager = new(ServiceGetter.getService<IModManager>);
    private static readonly Lazy<IExtraData_ServerManager> _IExtraData_ServerManager = new(ServiceGetter.getService<IExtraData_ServerManager>);
    private static readonly Lazy<ISenderShortcuts> _ISenderShortcuts = new(ServiceGetter.getService<ISenderShortcuts>);
    private static readonly Lazy<LaunchOptions> _LaunchOptions = new(ServiceGetter.getService<LaunchOptions>);
    private static readonly Lazy<ISaveManager> _ISaveManager = new(ServiceGetter.getService<ISaveManager>);
    private static readonly Lazy<IBackupManager> _IBackupManager = new(ServiceGetter.getService<IBackupManager>);
    private static readonly Lazy<PartialWorldTicker_TrimStalePartialWorlds> _PartialWorldTicker_TrimStalePartialWorlds = new(ServiceGetter.getService<PartialWorldTicker_TrimStalePartialWorlds>);
    private static readonly Lazy<IPlayerSaveManager> _IPlayerSaveManager = new(ServiceGetter.getService<IPlayerSaveManager>);
    private static readonly Lazy<IServer> _IServer = new(ServiceGetter.getService<IServer>);
    private static readonly Lazy<ExtraData> _ExtraData = new(ServiceGetter.getService<ExtraData>);
    private static readonly Lazy<IAutosaver> _IAutosaver = new(ServiceGetter.getService<IAutosaver>);
    private static readonly Lazy<PacketIndexMapSyncHelper> _PacketIndexMapSyncHelper = new(ServiceGetter.getService<PacketIndexMapSyncHelper>);
    private static readonly Lazy<IBuildingManager> _IBuildingManager = new(ServiceGetter.getService<IBuildingManager>);
    private static readonly Lazy<ComponentActionBuildRequestManager> _ComponentActionBuildRequestManager = new(ServiceGetter.getService<ComponentActionBuildRequestManager>);
    private static readonly Lazy<ILogicManager> _ILogicManager = new(ServiceGetter.getService<ILogicManager>);
    private static readonly Lazy<CircuitStates> _CircuitStates = new(ServiceGetter.getService<CircuitStates>);
    private static readonly Lazy<CommandConsole> _CommandConsole = new(ServiceGetter.getService<CommandConsole>);
    private static readonly Lazy<IComponentRestrictions> _IComponentRestrictions = new(ServiceGetter.getService<IComponentRestrictions>);
    private static readonly Lazy<ILocalNetworkAnnounceService> _ILocalNetworkAnnounceService = new(ServiceGetter.getService<ILocalNetworkAnnounceService>);
    private static readonly Lazy<Sender> _Sender = new(ServiceGetter.getService<Sender>);
    private static readonly Lazy<IHailHandler> _IHailHandler = new(ServiceGetter.getService<IHailHandler>);
    private static readonly Lazy<Frontend> _Frontend = new(ServiceGetter.getService<Frontend>);
    private static readonly Lazy<IServerPartialWorldsManager> _IServerPartialWorldsManager = new(ServiceGetter.getService<IServerPartialWorldsManager>);
    private static readonly Lazy<PartialWorldTicker_CullStalePartialWorldActions> _PartialWorldTicker_CullStalePartialWorldActions = new(ServiceGetter.getService<PartialWorldTicker_CullStalePartialWorldActions>);
    private static readonly Lazy<IGameConfig> _IGameConfig = new(ServiceGetter.getService<IGameConfig>);
    private static readonly Lazy<IExtraData_FileManager> _IExtraData_FileManager = new(ServiceGetter.getService<IExtraData_FileManager>);
    private static readonly Lazy<IReceiver> _IReceiver = new(ServiceGetter.getService<IReceiver>);
    private static readonly Lazy<IPlayerManager> _IPlayerManager = new(ServiceGetter.getService<IPlayerManager>);
    private static readonly Lazy<IChatManager> _IChatManager = new(ServiceGetter.getService<IChatManager>);
    private static readonly Lazy<Paths> _Paths = new(ServiceGetter.getService<Paths>);

    public static readonly Lazy<SelfUpdatingContainer<Cluster>> _ClusterContainer = new(ServiceGetter.getService<SelfUpdatingContainer<Cluster>>);
    public static readonly Lazy<SelfUpdatingContainer<ClusterLinker>> _ClusterLinkerContainer = new(ServiceGetter.getService<SelfUpdatingContainer<ClusterLinker>>);
#endregion
}
