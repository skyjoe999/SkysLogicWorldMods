using System.Collections.Generic;
using JimmysUnityUtilities;
using LogicAPI.WorldDataMutations;

namespace SkysGeneralLib.Server.WorldMutations;

public static class WorldDataMutationExtension
{
  public static void ApplyAndSend(
        this WorldDataMutation mutation
    ) => Services.IWorldMutationManager.ApplyMutationLocallyAndQueueToSendUpdateToClients(mutation);
    // public static void Apply( // Readonly my arch foe, I'm to lazy for reflection rn
    //     this WorldDataMutation mutation
    // ) => mutation.ApplyMutation(Services.IWorldMutationManager.ServerWorldDataMutator);
    public static void ApplyAndSendAll( // Broadly unnecessary but it matches the build requests
    this IReadOnlyList<WorldDataMutation> requests
    ) => requests.ForEach(ApplyAndSend);
}