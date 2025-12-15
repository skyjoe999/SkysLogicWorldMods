using System;
using System.Reflection;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.Physics;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.BinaryStuff;
using SkysGeneralLib.Shared;

namespace SkysCircuitBoardPlacementWorkaround.Client;

public abstract class SemiCircuitBoard : CircuitBoard
{
    public abstract bool CanMoveOn(HitInfo info);
    // Just a helper function for the most likely use case
    protected bool IsFirstCollider(HitInfo info) => info.Hit.collider == GetBlockEntity().Collider;

}

// Would love to make this a generic pattern except I cant without c++ style templates
public abstract class SemiCircuitBoard<TData> : 
    SemiCircuitBoard, IComponentClientCode where TData : class, CircuitBoard.IData
{
    private readonly CustomDataManager<TData> DataManager;

    protected SemiCircuitBoard()
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
