using System;
using System.Linq;
using EccsLogicWorldAPI.Shared.AccessHelper;
using JimmysUnityUtilities;
using LogicAPI.Interfaces;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.BinaryStuff;
using SkysGeneralLib.Shared;
using SkysGeneralLib.Shared.TypeExtensions;

namespace SkysGeneralLib.Client.TypeExtensions;

public static class ComponentClientCodeExtension
{
    public static string FormatCustomData<T>(this ComponentClientCode<T> component) where T : class
    {
        var data = ComponentClientCodeExtension<T>.GetCustomDataManager(component) as CustomDataManager<T>;
        var dataDict = CDMInfoManipulator<T>.GetDataDict(data);
        return dataDict.Keys.ToArray().Convert(info => $"\t{info.Name}: {dataDict[info]}\n").Aggregate("\n");
    }
}

public static class ComponentClientCodeExtension<T> where T : class
{
    public static readonly Func<ComponentClientCode<T>, ICustomDataManager<T>>
        GetCustomDataManager =
            Delegator.createFieldGetter<ComponentClientCode<T>, ICustomDataManager<T>>(
                Fields.getPrivate(typeof(ComponentClientCode<T>), "DataManager")
            );
}
