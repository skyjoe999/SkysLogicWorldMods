using System;
using EccsLogicWorldAPI.Client.Hooks;
using LogicAPI.Client;
using LogicWorld;
using SkysChallengeSystem.Client.EditGUI;

namespace SkysChallengeSystem.Client;

public class SkysChallengeSystem_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        ChallengeManager.Logger = LoggerFactory.CreateLogger("Client" + nameof(ChallengeManager));
        
        // try
        // {
        //     ModRegistry.InstalledMods
        //         .Where(f => f.Manifest.Dependencies.Contains(Manifest.ID))
        //         .ForEach(ChallengeManager.RegisterChallenges);
        // }
        // catch (Exception e)
        // {
        //     SceneAndNetworkManager.TriggerErrorScreen(e);
        // }

        WorldHook.worldLoading += () =>
        {
            //This action is in Unity execution scope, errors must be caught manually:
            try
            {
                EditChallengePeg.initialize();
                EditChallengeBoard.initialize();
                EditChallengeDisplay.initialize();
                EditChallengeButton.initialize();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to initialize Sky's Challenge System GUIs");
                SceneAndNetworkManager.TriggerErrorScreen(e);
            }
        };
    }
}
