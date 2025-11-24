using FancyInput;
using LogicAPI.Client;
using SkysBoardTools.Client.Keybindings;

namespace SkysBoardTools.Client;

public class SkysBoardTools_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        CustomInput.Register<SkysBoardToolsContext, SkysBoardToolsTrigger>("SkysBoardTools");
    }
}
