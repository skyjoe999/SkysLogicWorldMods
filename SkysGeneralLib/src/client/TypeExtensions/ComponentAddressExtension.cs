using LogicAPI.Data;
using LogicWorld;
using LogicWorld.ClientWorldStuff;
using LogicWorld.Interfaces;

namespace SkysGeneralLib.Client.TypeExtensions;

public static class ComponentAddressExtension
{
    public static IComponentInWorld GetComponent(this ComponentAddress address, ClientWorld world = null)
        => (world ?? SceneAndNetworkManager.MainWorld).Data.Lookup(address);

    public static IComponentClientCode GetClientCode(this ComponentAddress address, ClientWorld world = null)
        => (world ?? SceneAndNetworkManager.MainWorld).Renderer.Entities.GetClientCode(address);

    public static IComponentClientCode GetClientCode(
        this ComponentAddress address,
        out IComponentInWorld component,
        ClientWorld world = null
    )
    {
        var client = address.GetClientCode();
        component = client?.Component ?? address.GetComponent();
        return client;
    }

    public static bool DescendsFrom(this ComponentAddress address, ComponentAddress other)
    {
        var parent = address;
        while ((parent = parent.GetComponent().Parent) != ComponentAddress.Empty)
            if (parent == other)
                return true;
        return false;
    }
}
