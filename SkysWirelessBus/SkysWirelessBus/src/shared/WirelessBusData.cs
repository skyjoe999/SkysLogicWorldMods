using JimmysUnityUtilities;

namespace SkysWirelessBus.Shared;


public interface IWirelessBusData
{
    public const int WirelessBusMaxInputs = 64;
    public const int WirelessBusDefaultInputs = 2;
    Color24 LabelColor { get; set; }
    string BusName { get; set; }
    int InputCount { get; set; }

}

public static class WirelessBusDataExtension
{
    public static void Initialize(this IWirelessBusData data)
    {
        data.LabelColor = new(27, 27, 27);
        data.BusName = "Bus";
        data.InputCount = IWirelessBusData.WirelessBusDefaultInputs;
    }
}

