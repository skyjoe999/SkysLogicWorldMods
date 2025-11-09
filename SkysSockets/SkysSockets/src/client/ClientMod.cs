using EccsLogicWorldAPI.Client.Hooks;
using LogicAPI.Client;
using LogicWorld;
using SkysSockets.Client.EditGUI;
using System;

namespace SkysSockets.Client;

public class SkysSockets_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        WorldHook.worldLoading += () =>
        {
            //This action is in Unity execution scope, errors must be caught manually:
            try
            {
                EditMultiSocket.initialize();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to initialize Skys Sockets GUIs:");
                SceneAndNetworkManager.TriggerErrorScreen(e);
            }
        };
    }
}