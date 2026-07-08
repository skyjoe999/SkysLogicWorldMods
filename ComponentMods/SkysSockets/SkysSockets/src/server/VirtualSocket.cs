using EccsLogicWorldAPI.Server;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Server.Components;
using LogicAPI.Services;
using LogicAPI.WorldDataMutations;
using LogicWorld.LogicCode;
using LogicWorld.Server.Circuitry;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysSockets.Server;

public class VirtualSocket : Socket
{
    private static readonly IWorldDataMutator iWorldDataMutator;
    private static readonly ICircuitryManager iCircuitryManager;

    static VirtualSocket()
    {
        iWorldDataMutator = ServiceGetter.getService<IWorldDataMutator>();
        iCircuitryManager = ServiceGetter.getService<ICircuitryManager>();
    }

    private readonly LogicComponent Parent;
    private readonly Vector3 RelativePos;
    private readonly Quaternion RelativeRot;

    public VirtualSocket(
        IInputPeg Input,
        LogicComponent Parent,
        Vector3 RelativePos,
        Quaternion RelativeRot,
        (Vector3, Vector3, Vector3, Vector3) blueSquarePoints
    )
    {
        this.Parent = Parent;
        this.RelativePos = RelativePos;
        this.RelativeRot = RelativeRot;
        SkysSockets_ServerMod.FudgeInputs(this, Input);
        SkysSockets_ServerMod.SetComponent(this, GenerateComponent());
        SetBlueSquarePoints(blueSquarePoints);
        base.Initialize();
    }

    public override void OnComponentMoved()
    {
        var pos = Parent.Component.WorldPosition;
        pos += RelativePos.RotateAroundPivot(new Vector3(0, 0, 0), Parent.Component.WorldRotation);
        ((ComponentDataManager)Component).SetLocalPosition(pos);
        // Gods I hope those terms are in right order
        ((ComponentDataManager)Component).SetLocalRotation(Parent.Component.WorldRotation * RelativeRot);
        base.OnComponentMoved();
    }

    public void SetBlueSquarePoints((Vector3, Vector3, Vector3, Vector3) blueSquarePoints)
    {
        SetBlueSquarePoints([
            blueSquarePoints.Item1.x,
            blueSquarePoints.Item1.y,
            blueSquarePoints.Item1.z,
            blueSquarePoints.Item2.x,
            blueSquarePoints.Item2.y,
            blueSquarePoints.Item2.z,
            blueSquarePoints.Item3.x,
            blueSquarePoints.Item3.y,
            blueSquarePoints.Item3.z,
            blueSquarePoints.Item4.x,
            blueSquarePoints.Item4.y,
            blueSquarePoints.Item4.z,
        ]);
    }

    public void SetBlueSquarePoints(float[] blueSquarePoints) => SkysSockets_ServerMod.SetCodeInfoFloats(this, blueSquarePoints);

    // Hey, devs, maybe just dont look at this function
    private static IComponentInWorld GenerateComponent()
    {
        // Only needs to be done once but must be done after mod initalization
        // So its getting done everytime, sue me
        const string SocketTextId = "MHG.Socket";
        var _ComponentTypesManager = ServiceGetter.getService<ComponentTypesManager>();
        var SocketComponentType = new ComponentType(_ComponentTypesManager.GetNumericID(SocketTextId));

        // Oh... oh this is an evil hack
        var NewAddress = ComponentAddress.Empty;
        WorldMutation_AddSingleNewComponent mutation = new()
        {
            NewComponent = new ComponentData(SocketComponentType),
            AddressOfNewComponent = NewAddress,
        };

        iWorldDataMutator.AddSingleNewComponent(mutation);
        var newObject = iCircuitryManager.LookupComponent(NewAddress);
        var unmutation = new WorldMutation_RemoveComponentsAndChildrenAndAttachedWires
        {
            AddressesOfComponentsToRemove = [NewAddress]
        };
        iWorldDataMutator.RemoveComponentsAndChildrenAndAttachedWires(unmutation);
        return newObject.Component;
    }
}
