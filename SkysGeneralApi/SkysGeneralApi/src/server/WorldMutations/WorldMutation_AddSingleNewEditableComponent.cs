using LogicAPI.Data;
using LogicAPI.Services;
using LogicAPI.WorldDataMutations;
using UnityEngine;

namespace SkysGeneralLib.Server.WorldMutations;

public class WorldMutation_AddSingleNewEditableComponent : WorldMutation_AddSingleNewComponent
{
    public ComponentType ComponentType;
    public ComponentAddress Parent = default;
    public Vector3 LocalPosition = default;
    public Quaternion LocalRotation = default;
    public int InputCount = 0;
    public int OutputCount = 0;
    public byte[] CustomData = [];

    public string ComponentTextId
    {
        get => Services.ComponentTypesManager.GetTextID(ComponentType);
        set => ComponentType = new ComponentType(Services.ComponentTypesManager.GetNumericID(value));
    }

    public override void ApplyMutation(IWorldDataMutator mutator)
    {
        NewComponent = new ComponentData(ComponentType);
        IEditableComponentData data = NewComponent;
        data.Parent = Parent;
        data.LocalPosition = LocalPosition;
        data.LocalRotation = LocalRotation;
        data.CustomData = CustomData;

        var info = Services.ComponentTypesManager.GetComponentInfo(ComponentType);
        data.InputInfos = new InputInfo[info.PrefabIsDynamic ? InputCount : info.StaticPrefab.Inputs.Length];
        data.OutputInfos = new OutputInfo[info.PrefabIsDynamic ? OutputCount : info.StaticPrefab.Outputs.Length];

        mutator.AddSingleNewComponent(this);
    }
}