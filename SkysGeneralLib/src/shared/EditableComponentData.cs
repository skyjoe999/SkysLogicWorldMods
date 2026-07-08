using LogicAPI.Data;
using UnityEngine;

namespace SkysGeneralLib.Shared;

public class EditableComponentData(ComponentType type) : ComponentData(type)
{
    public new InputInfo[] InputInfos { get => (InputInfo[])base.InputInfos; set => ((IEditableComponentData)this).InputInfos = value; }
    public new OutputInfo[] OutputInfos { get => (OutputInfo[])base.OutputInfos; set => ((IEditableComponentData)this).OutputInfos = value; }
    public new byte[] CustomData { get => base.CustomData; set => ((IEditableComponentData)this).CustomData = value; }
    public new ComponentAddress Parent { get => base.Parent; set => ((IEditableComponentData)this).Parent = value; }
    public new Vector3Int LocalPositionFixed { get => base.LocalPositionFixed; set => ((IEditableComponentData)this).LocalPositionFixed = value; }
    public new Quaternion LocalRotation { get => base.LocalRotation; set => ((IEditableComponentData)this).LocalRotation = value; }
    public new Vector3 LocalPosition { get => ConvertFixedPositionToPosition(LocalPositionFixed); set => LocalPositionFixed = ConvertPositionToFixedPosition(value); }

    public EditableComponentData(ComponentData data) : this(data, data.Type) { }
    public EditableComponentData(IEditableComponentData data, ComponentType type) : this(type)
    {
        InputInfos = data.InputInfos;
        OutputInfos = data.OutputInfos;
        CustomData = data.CustomData;
        Parent = data.Parent;
        LocalPositionFixed = data.LocalPositionFixed;
        LocalRotation = data.LocalRotation;
        LocalPosition = data.LocalPosition;
    }
}
