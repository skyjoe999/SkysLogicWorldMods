using System;
using System.Linq;
using EccsLogicWorldAPI.Shared.AccessHelper;
using JimmysUnityUtilities;
using LogicAPI.Interfaces;
using LogicWorld.Server.Circuitry;
using LogicWorld.SharedCode.BinaryStuff;
using SkysGeneralLib.Shared;
using SkysGeneralLib.Shared.TypeExtensions;

namespace SkysGeneralLib.Server.TypeExtensions;

public static class LogicComponentExtension
{
    public static string FormatCustomData<T>(this LogicComponent<T> component) where T : class
    {
        var data = LogicComponentExtension<T>.GetCustomDataManager(component) as CustomDataManager<T>;
        var dataDict = CDMInfoManipulator<T>.GetDataDict(data);
        return dataDict.Keys.ToArray().Convert(info => $"\t{info.Name}: {dataDict[info]}").Aggregate("\n");
    }
}

public static class LogicComponentExtension<T> where T : class
{
    public static readonly Func<LogicComponent<T>, ICustomDataManager<T>>
        GetCustomDataManager =
            Delegator.createFieldGetter<LogicComponent<T>, ICustomDataManager<T>>(
                Fields.getPrivate(typeof(LogicComponent<T>), "DataManager")
            );
}
