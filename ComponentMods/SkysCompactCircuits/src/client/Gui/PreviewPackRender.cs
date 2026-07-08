using System.Reflection;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Building.Placement;
using LogicWorld.Building.Subassemblies;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.SharedCode.BinaryStuff;
using SkysCompactCircuits.Shared;
using UnityEngine;

namespace SkysCompactCircuits.Client.Gui;

public class PreviewPackRender()
{
    private static readonly PlacementData_Standard PlacementData = (PlacementData_Standard)
        typeof(PlacementData_Standard).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0]
        .Invoke([Vector3.zero, Quaternion.identity, PlacementType.Unknown, false]);
    public static SubassemblyData CreatePreview(IPackedCircuitData circuitData)
    {
        var cb = Instances.MainWorld.ComponentTypes.GetComponentType("MHG.CircuitBoard");
        var size = circuitData.Size;
        return new()
        {
            PartialWorldData = new(
                componentIDsMap: Instances.MainWorld.ComponentTypes.NumericIDsToTextIDs,
                orderedComponentsAndAddresses: [
                    (new(1), CircuitBoardData(size.x, size.y, new(0x333333), Vector3.zero)),
                    // edges
                    (new(2), CircuitBoardData(1, size.y, new(0x006DB0), new(-1, 0, 0), 1)),
                    (new(3), CircuitBoardData(1, size.y, new(0x006DB0), new(size.x, 0, 0), 1)),
                    (new(4), CircuitBoardData(size.x, 1, new(0x006DB0), new(0, 0, -1), 1)),
                    (new(5), CircuitBoardData(size.x, 1, new(0x006DB0), new(0, 0, size.y), 1)),
                    // corners
                    (new(6), CircuitBoardData(1, 1, new(0x333333), new(-1, 0, -1), 1)),
                    (new(7), CircuitBoardData(1, 1, new(0x333333), new(-1, 0, size.y), 1)),
                    (new(8), CircuitBoardData(1, 1, new(0x333333), new(size.x, 0, size.y), 1)),
                    (new(9), CircuitBoardData(1, 1, new(0x333333), new(size.x, 0, -1), 1)),
                    // the actual prefab!
                    (new(10), Main()),
                ],
                allWires: [],
                onStates: []
            ),
            RootPlacements = [PlacementData]
        };
        ComponentData CircuitBoardData(int sizeX, int sizeZ, Color24 color, Vector3 localPosition, uint parent = 0)
        {
            manager.Data.Color = color;
            manager.Data.SizeX = sizeX;
            manager.Data.SizeZ = sizeZ;
            var _data = new ComponentData(cb);
            var data = _data as IEditableComponentData;
            data.CustomData = manager.SerializeData();
            data.InputInfos = [];
            data.OutputInfos = [];
            data.LocalPosition = localPosition * 0.3f;
            data.Parent = new(parent);
            return _data;
        }
        ComponentData Main()
        {
            var _data = new ComponentData(Instances.MainWorld.ComponentTypes.GetComponentType(SkysCompactCircuits_ClientMod.PackedCircuitTextID));
            var data = _data as IEditableComponentData;
            data.CustomData = circuitData.Encode();
            data.InputInfos = new InputInfo[circuitData.InputCount];
            data.OutputInfos = new OutputInfo[circuitData.OutputCount];
            data.LocalPosition = new Vector3(0.5f, 0.5f, 0.5f) * 0.3f;
            data.Parent = new(1);
            return _data;
        }
    }
    private static readonly CustomDataManager<CircuitBoard.IData> manager = new();
}
