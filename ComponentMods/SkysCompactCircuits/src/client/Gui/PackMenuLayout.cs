using System;
using System.Linq;
using EccsGuiBuilder.Client.Components;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using EccsGuiBuilder.Client.Wrappers.Specialized;
using EccsLogicWorldAPI.Client.UnityHelper;
using EccsLogicWorldAPI.Shared;
using JimmysUnityUtilities;
using LogicSettings;
using LogicUI.MenuParts;
using LogicUI.MenuParts.Dropdowns;
using LogicUI.MenuParts.Toggles;
using LogicUI.Palettes;
using SkysGeneralLib.Shared.AccessTools;
using ThisOtherThing.UI.Shapes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkysCompactCircuits.Client.Gui;

// Separated into its own file because this is bloody massive!!!
public partial class PackMenu
{

    [AssignMe] public ImageController Renderer;
    [AssignMe] public TMP_InputField Size;
    [AssignMe] public TMP_InputField Offset;
    [AssignMe] public ToggleSwitch UseWidth;
    [AssignMe] public TMP_InputField Blocks;
    [AssignMe] public TMP_InputField Name;
    [AssignMe] public LocalizedDropdown PrefabDropdown;
    [AssignMe] public HoverButton SubmitButton;
    public static GameObject CustomElement;
    public static GameObject OffsetElement;

    public static void Build()
    {
        WS.window("SkysCompactCircuits.PackWindow")
            .setYPosition(870)
            .setLocalizedTitle("SkysCompactCircuits.Gui.PackMenu.Title")
            .setResizeable()
            .FixResizingArrows()
            .configureContent(content => content
                .layoutGrowElement(elementIndex: IndexHelper.First)
                .addContainer("Top", container => container
                    .layoutGrowElementHorizontalInner(elementIndex: IndexHelper.First)
                    .addContainer("Render", container => container
                        .layoutGrowElementVerticalInner(elementIndex: IndexHelper.Last)
                        .add(RecoloredTextField()
                            .setPlaceholderLocalizationKey("SkysCompactCircuits.Gui.PackMenu.Name")
                            .injectionKey(nameof(Name))
                            .fixedSize(0, 80)
                        )
                        .add(CustomWS.image
                            .addAndConfigure<LayoutElement>(layout => layout.minHeight = layout.minWidth = 30)
                            .add<ImageController>()
                            .injectionKey(nameof(Renderer))
                        )
                    )
                    .addContainer("Blocks", container => container
                        .addAndConfigure<Rectangle>(rect =>
                        {
                            rect.ShapeProperties.DrawOutline = true;
                            var pal = rect.AddComponent<PaletteGraphic>();
                            new Accessor<PaletteGraphic, Graphic>("Target").Set(pal, rect);
                            pal.SetPaletteColor(PaletteColor.Secondary);
                            var rectPal = rect.AddComponent<PaletteRectangleOutline>();
                            new Accessor<PaletteRectangleOutline, Rectangle>("Target").Set(rectPal, rect);
                            rectPal.SetPaletteColor(PaletteColor.Tertiary);
                        })
                        .layoutGrowElement(elementIndex: IndexHelper.nth(2))
                        .add(WS.textLine
                            .setLocalizationKey("SkysCompactCircuits.Gui.PackMenu.Blocks")
                            .fixedSize(0, 80)
                            .configureTMP(tmp => tmp.alignment = TextAlignmentOptions.Center)
                        )
                        .addContainer("Dropdown", container => BuildDropdown(container))
                        .addContainer("Custom", container => container
                            .layoutGrowElementVerticalInner()
                            .add(WS.inputArea
                                .fixedSize(300, 90) // effective minimum height just to stop the outline from breaking
                                .configure(tmp => tmp.text = "- Standard")
                                .injectionKey(nameof(Blocks))
                                .assignTo(out CustomElement)
                            )
                        )
                        .addContainer("Offset", container => container
                            .layoutGrowElementHorizontalInner(elementIndex: IndexHelper.nth(1), expandChildThickness: false)
                            .add(WS.textLine
                                .setLocalizationKey("SkysCompactCircuits.Gui.PackMenu.Offset")
                                .setFontSize(42)
                            )
                            .add(RecoloredTextField()
                                .configure(tmp =>
                                {
                                    tmp.contentType = TMP_InputField.ContentType.DecimalNumber;
                                    tmp.pointSize = 42;
                                })
                                .setPlaceholderText("0")
                                .fixedSize(50, 50)
                                .injectionKey(nameof(Offset))
                            )
                            .assignTo(out OffsetElement)
                        )
                        .addContainer("Size", container => BuildSizeInput(container))
                    )

                    // .addContainer("Addons", container => container
                    //     .addAndConfigure<Rectangle>(rect =>
                    //     {
                    //         // rect.ShapeProperties.DrawFill = false;
                    //         rect.ShapeProperties.DrawOutline = true;
                    //         var pal = rect.AddComponent<PaletteGraphic>();
                    //         new Accessor<PaletteGraphic, Graphic>("Target").Set(pal, rect);
                    //         pal.SetPaletteColor(PaletteColor.Secondary);
                    //         var rectPal = rect.AddComponent<PaletteRectangleOutline>();
                    //         new Accessor<PaletteRectangleOutline, Rectangle>("Target").Set(rectPal, rect);
                    //         rectPal.SetPaletteColor(PaletteColor.Tertiary);
                    //     })
                    //     .layoutGrowGap(gapIndex: IndexHelper.Last)
                    //     .addContainer("Setting", container => container
                    //         .layoutGrowGapHorizontalInner(gapIndex: IndexHelper.nth(1), expandChildThickness: false, anchor: Anchor.Center)
                    //         .add(WS.textLine.setFontSize(36).setLocalizationKey("Nested Addons"))
                    //         .add(CustomWS.wordlessToggle.fixedSize(40 * 1.7f, 40))
                    //     )
                    //     .addContainer("Setting", container => container
                    //         .layoutGrowGapHorizontalInner(gapIndex: IndexHelper.nth(1), expandChildThickness: false, anchor: Anchor.Center)
                    //         .add(WS.textLine.setFontSize(36).setLocalizationKey("Nested Blocks"))
                    //         .add(CustomWS.wordlessToggle.fixedSize(40 * 1.7f, 40))
                    //     )
                    // )
                )
                .addContainer("bottom", container => container
                    .layoutHorizontalInnerCentered()
                    .add(WS.button
                        .fixedSize(700, 150)
                        .setLocalizationKey("SkysCompactCircuits.Gui.PackMenu.Pack") // You are not getting a damn button icon out of me!
                        .injectionKey(nameof(SubmitButton))
                    )
                )
            )
            .add<PackMenu>()
            .build();
    }

    private static TextFieldWrapper RecoloredTextField()
    {
        var gameObject = CustomStore.genInputField;
        gameObject.GetComponent<PaletteGraphic>()
            .SetPaletteColor(PaletteColor.Secondary);
        gameObject.transform.GetChild(0).GetChild(2).gameObject.GetComponent<PaletteGraphic>()
            .SetPaletteColor(PaletteColor.Text_Primary); // Placeholder
        gameObject.transform.GetChild(0).GetChild(3).gameObject.GetComponent<PaletteGraphic>()
            .SetPaletteColor(PaletteColor.Text_Primary); // Text
        return new(gameObject);
    }
    private static SimpleWrapper BuildSizeInput(SimpleWrapper container)
    {

        return container
            .layoutGrowElementHorizontalInner(elementIndex: IndexHelper.nth(1), expandChildThickness: false)
            .add(WS.textLine
                .setLocalizationKey("SkysCompactCircuits.Gui.PackMenu.Size")
                .setFontSize(42)
            )
            .add(RecoloredTextField()
                .configure(tmp =>
                {
                    tmp.contentType = TMP_InputField.ContentType.IntegerNumber;
                    tmp.pointSize = 42;
                })
                .setPlaceholderText("1")
                .fixedSize(50, 50)
                .injectionKey(nameof(Size))
            )
            .addContainer("Spacer", container => container.fixedSize(20, 0))
            .add(WS.textLine
                .setLocalizationKey("SkysCompactCircuits.Gui.PackMenu.Z")
                .setFontSize(42)
            )
            .add(CustomWS.wordlessToggle
                .injectionKey(nameof(UseWidth))
                .fixedSize(90, 50)
            )
            .add(WS.textLine
                .setLocalizationKey("SkysCompactCircuits.Gui.PackMenu.X")
                .setFontSize(42)
            )
        ;
    }
    private static SimpleWrapper BuildDropdown(SimpleWrapper container)
    {
        var settings = GameObjectQuery.queryGameObject("Settings Menu");
        NullChecker.check(settings, "Could not find Settings Menu");
        var dropdown = new Accessor<SettingsMenu, GameObject>("SettingPrefab_DropdownEnum")
            .Get(settings.GetComponent<SettingsMenu>())
            .GetComponentInChildren<LocalizedDropdown>()
            .gameObject.clone().GetComponent<LocalizedDropdown>();
        dropdown.RemoveComponentImmediate<LocalizedEnumDropdown>();
        new Accessor<Dropdown<LocalizedDropdownItem>, GameObject>("DropdownItemPrefab").Get(dropdown)
            .GetComponentInChildren<TextMeshProUGUI>().fontSizeMax = 30;
        new Accessor<LocalizedDropdown, string[]>("StartingLocalizationKeys").Set(
            dropdown,
            [.. Enum.GetNames(typeof(PrefabModes)).Select(name => $"SkysCompactCircuits.Gui.PackMenu.PrefabModes.{name}")]
        );
        return container
            .layoutHorizontalInner()
            .add(new SimpleWrapper(dropdown.gameObject))
            .fixedSize(0, 60)
            .injectionKey(nameof(PrefabDropdown))
        ;
    }
}
