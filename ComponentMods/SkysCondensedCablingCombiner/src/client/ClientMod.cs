using System;
using EccsLogicWorldAPI.Client.Hooks;
using LogicAPI.Client;
using LogicWorld;
using LogicWorld.Rendering.Components;
using SkysCondensedCablingCombiner.Shared;

namespace SkysCondensedCablingCombiner.Client;

public class SkysCondensedCablingCombiner_ClientMod : ClientMod
{
    public override void Initialize()
    {
        WorldHook.worldLoading += () =>
        {
            //This action is in Unity execution scope, errors must be caught manually:
            try { EditCombiner.Build(); }
            catch (Exception e)
            {
                Logger.Error($"Failed to initialize GUI for {Manifest.Name}:");
                SceneAndNetworkManager.TriggerErrorScreen(e);
            }
        };
    }
}

class Combiner : ComponentClientCode<ICombinerData>
{
    public override void SetDataDefaultValues() => Data.Initialize();
}
