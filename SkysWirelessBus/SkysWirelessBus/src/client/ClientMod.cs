using EccsLogicWorldAPI.Client.Hooks;
using LogicAPI.Client;
using LogicWorld;
using SkysWirelessBus.Client.EditGUI;
using System;

namespace SkysWirelessBus.Client;
public class SkysWirelessBus_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        WorldHook.worldLoading += () =>
        {
            //This action is in Unity execution scope, errors must be caught manually:
            try
            {
                EditWirelessBus.initialize();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to initialize Eccs Component Edit GUIs:");
                SceneAndNetworkManager.TriggerErrorScreen(e);
            }
        };
    }
}
