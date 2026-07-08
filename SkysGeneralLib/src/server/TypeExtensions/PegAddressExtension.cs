using System.Linq;
using LogicAPI.Data;

namespace SkysGeneralLib.Server.TypeExtensions;

public static class PegAddressExtension
{
    public static bool GetOn(this PegAddress address)
        => address.IsInputAddress()
            ? address.ComponentAddress.GetLogicComponent().Inputs[address.PegIndex].On
            : address.ComponentAddress.GetLogicComponent().Outputs[address.PegIndex].On;
    public static IPegInfo GetInfo(this PegAddress address)
        => address.IsInputAddress()
            ? address.ComponentAddress.GetComponent().Data.InputInfos.Cast<InputInfo?>().ElementAtOrDefault(address.PegIndex)
            : address.ComponentAddress.GetComponent().Data.OutputInfos.Cast<OutputInfo?>().ElementAtOrDefault(address.PegIndex);

    public static int GetStateID(this PegAddress address)
        => address.GetInfo().StateID;
    public static int? GetStateIDOrNull(this PegAddress address)
        => address.GetInfo()?.StateID;
}
