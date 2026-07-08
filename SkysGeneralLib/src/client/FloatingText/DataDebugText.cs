using System;
using EccsLogicWorldAPI.Shared.AccessHelper;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.BinaryStuff;
using UnityEngine;

namespace SkysGeneralLib.Client.FloatingText;

public class DataDebugText<TData> : FloatingText where TData : class
{
    private readonly Action<(ComponentClientCode<TData>, DataDebugText<TData>)> OnPropertyUpdateAction;
    private readonly ComponentClientCode<TData> instance;

    public DataDebugText(
        ComponentClientCode<TData> instance,
        Action<(ComponentClientCode<TData>, DataDebugText<TData>)> onPropertyUpdateAction,
        Transform parentToCreateDecorationsUnder
    ) : base(parentToCreateDecorationsUnder)
    {
        this.instance = instance;
        OnPropertyUpdateAction = onPropertyUpdateAction;
        DataManager.OnPropertySet += OnPropertyUpdate;
    }

    private void OnPropertyUpdate()
    {
        OnPropertyUpdateAction((instance, this));
    }

    protected CustomDataManager<TData> DataManager => GetDataManager_Func(instance);

    private static readonly Func<ComponentClientCode<TData>, CustomDataManager<TData>> GetDataManager_Func =
        Delegator.createFieldGetter<ComponentClientCode<TData>, CustomDataManager<TData>>(
            Fields.getPrivate(typeof(ComponentClientCode<TData>), "DataManager"));
}
