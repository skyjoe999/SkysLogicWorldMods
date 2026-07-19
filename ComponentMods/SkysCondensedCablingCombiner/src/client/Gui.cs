using System;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using LogicAPI.Data.BuildingRequests;
using LogicUI.MenuParts;
using LogicWorld.UI;
using SkysCondensedCablingCombiner.Shared;
using SkysGeneralLib.Client.BuildRequests;

namespace SkysCondensedCablingCombiner.Client;

public class EditCombiner : EditComponentMenu<ICombinerData>, IAssignMyFields
{
    public static void Build()
    {
        WS.window("EditCombinerWindow")
            .setYPosition(870)
            .setMinSize(800, 0)
            .setDefaultSize(800, 0)
            .configureContent(content => content
                .layoutVertical()
                .add(WS.textLine
                    .setLocalizationKey("SkysCondensedCablingCombiner.Gui.EditCombiner.PegCount")
                    .setFontSize(35)
                )
                .add(WS.slider
                    .injectionKey(nameof(PegCount))
                    .setMin(2)
                    .setMax(16)
                    .fixedSize(400, 38)
                )
                .add(WS.textLine
                    .setLocalizationKey("SkysCondensedCablingCombiner.Gui.EditCombiner.BitCount")
                    .setFontSize(40)
                )
                .add(WS.slider
                    .injectionKey(nameof(BitCount))
                    .setMin(1)
                    .setMax(32)
                    .fixedSize(400, 38)
                )
            )
            .add<EditCombiner>()
            .build();
    }

    [AssignMe] public InputSlider PegCount;
    [AssignMe] public InputSlider BitCount;

    public override void Initialize()
    {
        base.Initialize();

        //Setup events and handlers:
        PegCount.OnValueChanged += value =>
        {
            var maxBitCount = (int)MathF.Floor(256f / value);
            foreach (var entry in ComponentsBeingEdited)
            {
                new BuildRequest_ChangeDynamicComponentPegCounts(entry.Address, (int)value + 1, 0).SendNoUndo();
                entry.Data.BitsPerInput = Math.Min(maxBitCount, entry.Data.BitsPerInput);
            }
            BitCount.SetValueWithoutNotify(Math.Min(maxBitCount, BitCount.ValueAsInt));
            BitCount.Max = maxBitCount;
        };
        BitCount.OnValueChanged += value =>
        {
            foreach (var entry in ComponentsBeingEdited)
            {
                entry.Data.BitsPerInput = (int)value;
            }

        };
    }

    public override void OnStartEditing()
    {
        PegCount.SetValueWithoutNotify(FirstComponentBeingEdited.Component.Data.InputCount - 1);
        BitCount.SetValueWithoutNotify(FirstComponentBeingEdited.Data.BitsPerInput);
        BitCount.Max = (int)MathF.Floor(256f / PegCount.Value);
    }
}
