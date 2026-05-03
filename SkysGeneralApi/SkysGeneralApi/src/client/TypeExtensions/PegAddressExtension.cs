using System.Linq;
using LogicAPI.Data;
using LogicWorld.ClientWorldStuff;

namespace SkysGeneralLib.Client.TypeExtensions;

public static class PegAddressExtension
{
    public static IPegInfo GetInfo(this PegAddress address, ClientWorld world = null)
        => address.IsInputAddress()
            ? address.ComponentAddress.GetComponent(world)?.Data.InputInfos.Cast<InputInfo?>().ElementAtOrDefault(address.PegIndex)
            : address.ComponentAddress.GetComponent(world)?.Data.OutputInfos.Cast<OutputInfo?>().ElementAtOrDefault(address.PegIndex);

    public static int GetStateID(this PegAddress address, ClientWorld world = null)
        => address.GetInfo(world).StateID;
    public static int? GetStateIDOrNull(this PegAddress address, ClientWorld world = null)
        => address.GetInfo(world)?.StateID;
}
