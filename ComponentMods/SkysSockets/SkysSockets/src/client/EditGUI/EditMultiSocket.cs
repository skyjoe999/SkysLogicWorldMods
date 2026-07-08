using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using LogicAPI.Data.BuildingRequests;
using LogicUI.MenuParts;
using LogicWorld.BuildingManagement;
using LogicWorld.UI;
using System.Linq;
using UnityEngine;

namespace SkysSockets.Client.EditGUI;

public class EditMultiSocket : EditComponentMenu<ClientCode.MultiSocket.IData>, IAssignMyFields
{
    public static void initialize()
    {
        WS.window("SkysMultiSocketEditMultiSocketWindow")
            .setYPosition(870)
            .configureContent(content => content
                .layoutVertical()
                .addContainer("MainContentBox", mainContent => mainContent
                    .layoutVertical(spacing: 10, padding: new RectOffset(20, 20, 10, 20)) // Make the gap between label and element smaller and compensate for text-space at the top.
                    .add(WS.textLine
                        .setLocalizationKey("SkysMultiSocket.Gui.MultiSocket.InputCountLabel")
                        .setFontSize(25)
                    )
                    .add(WS.slider
                        .injectionKey(nameof(valueSlider))
                        .setMin(1)
                        .setMax(Shared.MultiSocket.MultiSocketMaxInputs)
                        .fixedSize(200, 38)
                    )
                )
            )
            .add<EditMultiSocket>()
            .build();
    }
    
    //Instance part:
    [AssignMe]
    public InputSlider valueSlider;
    
    public override void Initialize()
    {
        base.Initialize();
        
        //Setup events and handlers:
        valueSlider.OnValueChanged += value =>
        {
            foreach (var entry in ComponentsBeingEdited)
                BuildRequestManager.SendBuildRequest(new BuildRequest_ChangeDynamicComponentPegCounts(entry.Address, (int)value, 0));
            
        };
    }

    protected override void OnStartEditing()
    {
        valueSlider.SetValueWithoutNotify(ComponentsBeingEdited.First().Component.Data.InputCount);
    }
}
