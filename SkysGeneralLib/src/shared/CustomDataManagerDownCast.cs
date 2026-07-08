using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EccsLogicWorldAPI.Shared.AccessHelper;
using LogicWorld.SharedCode.BinaryStuff;

namespace SkysGeneralLib.Shared;

public class CustomDataManagerDownCast<TData, OldTData> : CustomDataManager<OldTData>
    where OldTData : class
    where TData : class, OldTData
{
    public readonly CustomDataManager<TData> BaseDataManager;
    public new TData Data => BaseDataManager.Data;

    public CustomDataManagerDownCast(CustomDataManager<TData> BaseDataManager)
    {
        this.BaseDataManager = BaseDataManager;
        CDMInfoManipulator<OldTData>.SetDataDict(this, CDMInfoManipulator<TData>.GetDataDict(BaseDataManager));
        OnPropertySet += BaseOnPropertySet;
    }

    public new byte[] SerializeData(int expectedDataLength = 16) => BaseDataManager.SerializeData(expectedDataLength);
    public new bool TryDeserializeData(byte[] data) => BaseDataManager.TryDeserializeData(data);

    private static readonly PropertyInfo firstInfo = CDMInfoManipulator<TData>.GetDataPropertiesCache().First();
    private void BaseOnPropertySet() => firstInfo.SetValue(BaseDataManager.Data, firstInfo.GetValue(BaseDataManager.Data));
}

public static class CDMInfoManipulator<TData> where TData : class
{
    public static readonly Func<CustomDataManager<TData>, IDictionary<PropertyInfo, object>>
        GetDataDict =
            Delegator.createFieldGetter<CustomDataManager<TData>, IDictionary<PropertyInfo, object>>(
                Fields.getPrivate(typeof(CustomDataManager<TData>), "DataDic")
            );

    public static readonly Action<CustomDataManager<TData>, IDictionary<PropertyInfo, object>> SetDataDict =
        Fields.getPrivate(typeof(CustomDataManager<TData>), "DataDic").SetValue;

    public static readonly Func<PropertyInfo[]> GetDataPropertiesCache =
        Delegator.createStaticFieldGetter<PropertyInfo[]>(
            Fields.getPrivateStatic(typeof(CustomDataManager<TData>), "DataPropertiesCache")
        );
}
