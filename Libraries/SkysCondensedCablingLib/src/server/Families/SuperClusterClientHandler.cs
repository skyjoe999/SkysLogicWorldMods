using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Networking;
using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using SkysCondensedCablingLib.Shared;
using SkysGeneralLib.Server;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysCondensedCablingLib.Server;

public static class SuperClusterClientHandler
{
    #region Connect any
    public static readonly HashSet<ComponentType> ConnectAnyComponentIDs = [];

    // Anything in this list should not allow you to connect two clusters in any way execpt by adding a wire and has no output logic.
    // So that means nothing like relays, fast buffers, sockets, etc. Basically just pegs and through pegs.
    public static void RegisterConnectAnyType(string textID)
    {
        ConnectAnyComponentIDs.Add(Services.ComponentTypesManager.GetComponentType(textID));
    }

    public static void SendSetupAny(Cluster cluster)
    {
        if (ClusterCanConnectAny(cluster))
            Services.NetworkServer.Broadcast(new UpdateSuperClusterPacket() { StateID = cluster.StateID, ConnectionID = -2 });
    }

    public static void SendCleanupAny(Cluster cluster)
    {
        if (ClusterCanConnectAny(cluster))
            Services.NetworkServer.Broadcast(new UpdateSuperClusterPacket() { StateID = cluster.StateID });
    }

    public static bool ClusterCanConnectAny(Cluster cluster)
    {
        return 
            cluster is not SuperCluster and not null && // Can't be a SuperCluster.
            cluster.ConnectedOutputs.Count == 0 && // Can't have outputs.
            cluster.CircuitStates == Services.CircuitStates && // Can't be inside a SuperCluster.
            cluster.ConnectedInputs.All(input => ConnectAnyComponentIDs.Contains(input.Address.ComponentAddress.GetComponent().Data.Type));
    }
    #endregion

    #region Super
    public static void SendSetup(SuperCluster cluster)
    {
        if (cluster.Family is { } family)
            Services.NetworkServer.Broadcast(new UpdateSuperClusterPacket() { StateID = cluster.StateID, Color = family.TryGetValue(cluster.Size, out var color) ? color : null, ConnectionID = family.ConnectionID });
    }

    public static void SendSetup(SuperOutputPeg output)
    {
        if (output.Family is { } family)
            Services.NetworkServer.Broadcast(new UpdateSuperClusterPacket() { StateID = output.StateID, Color = family.TryGetValue(output.Size, out var color) ? color : null, ConnectionID = family.ConnectionID });
    }

    public static void SendCleanup(SuperCluster cluster)
    {
        if (cluster.Family is not null)
            Services.NetworkServer.Broadcast(new UpdateSuperClusterPacket() { StateID = cluster.StateID });
    }

    public static void SendCleanup(SuperOutputPeg output)
    {
        if (output.Family is not null)
            Services.NetworkServer.Broadcast(new UpdateSuperClusterPacket() { StateID = output.StateID });
    }

    public static void SetupPlayer(Connection connection, ICollection<LogicComponent> components)
    {
        var clusters = components.SelectMany(component => component.Inputs
                .Select(input => (input as InputPeg)?.Cluster as SuperCluster)
                .Where(cluster => cluster is { Family: not null })
            )
            .Distinct()
            .Select(c => (c.StateID, Color: c.Family.TryGetValue(c.Size, out var color) ? color : ((Color24, Color24)?)null, c.Family.ConnectionID));
        var outputs = components.SelectMany(component => component.Outputs
            .Select(output => output as SuperOutputPeg)
            .Where(output => output is { Family: not null })
            .Select(output => (output.StateID, Color: output.Family.TryGetValue(output.Size, out var color) ? color : ((Color24, Color24)?)null, output.Family.ConnectionID))
        );
        var connectAny = components.SelectMany(component => component.Inputs)
            .Select(input => (input as InputPeg)?.Cluster)
            .Distinct()
            .Where(ClusterCanConnectAny)
            .Select(cluster => (cluster.StateID, Color: ((Color24, Color24)?)null, -2));

        Services.NetworkServer.Send(connection, new BulkSuperClusterPacket() { values = [.. clusters, .. outputs, .. connectAny] });
    }
    #endregion
}
