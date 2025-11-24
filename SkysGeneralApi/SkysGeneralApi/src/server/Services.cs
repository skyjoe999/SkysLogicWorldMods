using EccsLogicWorldAPI.Server;
using LogicAPI.Server.Managers;
using LogicAPI.Server.Networking;
using LogicAPI.Services;
using LogicWorld.Server;
using LogicWorld.Server.Circuitry;
using LogicWorld.Server.HostServices;
using LogicWorld.Server.Managers;
using LogicWorld.SharedCode.Components;

namespace SkysGeneralLib.Server;

public static class Services
{
    public static readonly ComponentTypesManager ComponentTypesManager;
    public static readonly ICircuitryManager ICircuitryManager;
    public static readonly IClusterFactory IClusterFactory;
    public static readonly INetworkManager INetworkManager;
    public static readonly ISimulationManager ISimulationManager;
    public static readonly IWorldData IWorldData;
    public static readonly IWorldDataMutator IWorldDataMutator;
    public static readonly IWorldMutationManager IWorldMutationManager;
    public static readonly IWorldUpdates IWorldUpdates;
    public static readonly NetworkServer NetworkServer;

    static Services()
    {
        ComponentTypesManager = ServiceGetter.getService<ComponentTypesManager>();
        ICircuitryManager = ServiceGetter.getService<ICircuitryManager>();
        IClusterFactory = ServiceGetter.getService<IClusterFactory>();
        INetworkManager = ServiceGetter.getService<INetworkManager>();
        ISimulationManager = ServiceGetter.getService<ISimulationManager>();
        IWorldData = ServiceGetter.getService<IWorldData>();
        IWorldDataMutator = ServiceGetter.getService<IWorldDataMutator>();
        IWorldMutationManager = ServiceGetter.getService<IWorldMutationManager>();
        IWorldUpdates = ServiceGetter.getService<IWorldUpdates>();
        NetworkServer = ServiceGetter.getService<NetworkServer>();
    }
}