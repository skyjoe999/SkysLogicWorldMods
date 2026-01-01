using System.Linq;
using JimmysUnityUtilities;
using LogicWorld.SharedCode.BinaryStuff;

namespace SkysGeneralLib.Shared.TypeExtensions;

public static class CustomDataManagerExtension
{
    public static string format<T>(this CustomDataManager<T> data) where T : class
    {
        var dataDict = CDMInfoManipulator<T>.GetDataDict(data);
        return dataDict.Keys.ToArray().Convert(info => $"\t{info.Name}: {dataDict[info]}").Aggregate("\n");
    }

    public static string format<T>(this CustomDataManager<T> data, int maxArrayLength) where T : class
    {
        var dataDict = CDMInfoManipulator<T>.GetDataDict(data);
        return dataDict.Keys.ToArray().Convert(info =>
                $"\t{info.Name}: "
                + (dataDict[info] is byte[] arr ? arr.format(maxArrayLength) : dataDict[info].ToString())
            )
            .Aggregate("\n");
    }
}
