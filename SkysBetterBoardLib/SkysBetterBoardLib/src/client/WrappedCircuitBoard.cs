using System;
using System.Reflection;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.BinaryStuff;
using SkysGeneralLib.Shared;

namespace SkysBetterBoardLib.Client;

// Would love to make this a generic pattern except I cant without c++ style templates
public abstract class WrappedCircuitBoard<TData> :
    CircuitBoard, IComponentClientCode where TData : class, CircuitBoard.IData
{
    private readonly CustomDataManager<TData> DataManager;

    protected WrappedCircuitBoard()
    {
        DataManager = new CustomDataManager<TData>();
        typeof(ComponentClientCode<IData>)
            .GetField("DataManager", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(this, new CustomDataManagerDownCast<TData, IData>(DataManager));
        DataManager.OnPropertySet += (Action)(() =>
        {
            var numArray = SerializeCustomData();
            LoadCustomDataClientSide(numArray);
            if (!PlacedInMainWorld)
                return;
            BuildRequestManager.SendBuildRequestWithoutAddingToUndoStack(
                new BuildRequest_UpdateComponentCustomData(Address, numArray));
        });
    }

    public new TData Data => DataManager.Data;

    object IComponentClientCode.CustomDataObject => Data;
    public override byte[] SerializeCustomData() => DataManager.SerializeData();

    protected override void DeserializeData(byte[] data)
    {
        if (data == null)
            SetDefaultCustomData();
        else if (!DataManager.TryDeserializeData(data))
            throw new Exception(
                $"Error deserializing data for component at {Address} of type {Component?.Data.Type}");
    }

    private void SetDefaultCustomData() => SetDataDefaultValues();

    protected abstract override void SetDataDefaultValues();

    private void LoadCustomDataClientSide(byte[] data, bool queueDataUpdate = true)
    {
        DeserializeData(data);
        if (!queueDataUpdate)
            return;
        QueueDataUpdate();
    }
}
