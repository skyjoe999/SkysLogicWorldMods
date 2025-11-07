using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using LogicAPI.Data.BuildingRequests;
using LogicUI.ColorChoosing;
using LogicUI.MenuParts;
using LogicWorld.BuildingManagement;
using LogicWorld.UI;
using SkysWirelessBus.Shared;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SkysWirelessBus.Client.EditGUI;

public class EditWirelessBus : EditComponentMenu<IWirelessBusData>, IAssignMyFields
{
	public static void initialize()
	{
		WS.window("SkysWirelessBusEditWirelessBusWindow")
			.setYPosition(870)
			.configureContent(content => content
				.layoutVertical()
				.addContainer("TopBarBox", topBar => topBar
					.layoutHorizontalInner()
					.add(WS.textLine.setLocalizationKey("SkysWirelessBus.Gui.WirelessBus.BusNameLabel"))
					
                        .add(WS.inputArea
                            .injectionKey(nameof(busNameInput))
                            .fixedSize(400, 70)
                            .fixTextAreaColor()
                            .setPlaceholderLocalizationKey("SkysWirelessBus.Gui.WirelessBus.BusNameHint")
                            .configure(inputField => {
                                inputField.lineType = TMP_InputField.LineType.SingleLine;
                                var text = inputField.textComponent;
                                text.textWrappingMode = TextWrappingModes.NoWrap;
							
                                text.fontSize = 29;
                                var placeholder = (TMP_Text)inputField.placeholder;
                                placeholder.fontSizeMax = 29;
                                placeholder.textWrappingMode = TextWrappingModes.NoWrap;
                            })
                        )
                        .add(WS.colorPicker
                            .injectionKey(nameof(labelColorPicker))
                            .fixedSize(140, 70)
                        )
                    )
				.addContainer("MainContentBox", mainContent => mainContent
                        .layoutVertical(spacing: 10, padding: new RectOffset(20, 20, 10, 20)) // Make the gap between label and element smaller and compensate for text-space at the top.
					.add(WS.textLine
						.setLocalizationKey("SkysWirelessBus.Gui.WirelessBus.InputCountLabel")
						.setFontSize(25)
					)
					.add(WS.slider
						.injectionKey(nameof(valueSlider))
						.setMin(1)
						.setMax(IWirelessBusData.WirelessBusMaxInputs)
                            .fixedSize(200, 38)
                        )
				)
			)
			.add<EditWirelessBus>()
			.build();
	}
	
	//Instance part:
	
	[AssignMe]
	public ColorChooser labelColorPicker;
	[AssignMe]
        public InputSlider valueSlider;
        [AssignMe]
	public TMP_InputField busNameInput;
	
	public override void Initialize()
	{
		base.Initialize();
		
		//Setup events and handlers:
		
		labelColorPicker.OnColorChange24 += color => {
			foreach(var entry in ComponentsBeingEdited)
			{
				entry.Data.LabelColor = color;
			}
		};
		valueSlider.OnValueChanged += value =>
		{
			foreach (var entry in ComponentsBeingEdited)
			{
				entry.Data.InputCount = (int)value;
				BuildRequestManager.SendBuildRequest(new BuildRequest_ChangeDynamicComponentPegCounts(entry.Address, (int)value, 0));
			}
		};
            busNameInput.onValueChanged.AddListener(label => {
			foreach(var entry in ComponentsBeingEdited)
			{
				entry.Data.BusName = label;
			}
		});
	}
	
	protected override void OnStartEditing()
	{
		var data = FirstComponentBeingEdited.Data;
		labelColorPicker.SetColorWithoutNotify(data.LabelColor.WithAlphaChannel());
		busNameInput.text = data.BusName ?? "";
            valueSlider.SetValueWithoutNotify(base.ComponentsBeingEdited.First().Component.Data.InputCount);
        }
}
