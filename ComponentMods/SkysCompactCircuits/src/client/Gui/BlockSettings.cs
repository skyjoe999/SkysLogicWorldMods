using System.Collections.Generic;
using EccsGuiBuilder.Client.Components;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.Specialized;
using EccsLogicWorldAPI.Client.UnityHelper;
using EccsLogicWorldAPI.Shared;
using JimmysUnityUtilities;
using LogicInitializable;
using LogicSettings;
using LogicUI.MenuParts.Dropdowns;
using LogicUI.Palettes;
using LogicWorld.References;
using SkysGeneralLib.Shared.AccessTools;
using ThisOtherThing.UI.Shapes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkysCompactCircuits.Client.Gui;

// Currently this is unused because I ran out of steam because ui is awful!
// but if someone else wants to finish it up, the actual ui part is almost done, just need to add hooks for all the parts updating
// (and add an add button, and make it scrollable)
class BlockSettings : MonoBehaviour, IInitializable
{
    public static GameObject CreateMenu()
    {
        var settings = GameObjectQuery.queryGameObject("Settings Menu");
        NullChecker.check(settings, "Could not find Settings Menu");
        var dropdown = new Accessor<SettingsMenu, GameObject>("SettingPrefab_DropdownDynamic")
            .Get(settings.GetComponent<SettingsMenu>())
            .GetComponentInChildren<TextDropdown>()
            .gameObject.clone().GetComponent<TextDropdown>();
        new Accessor<Dropdown<TextDropdownItem>, GameObject>("DropdownItemPrefab").Get(dropdown)
            .GetComponentInChildren<TextMeshProUGUI>().fontSizeMax = 30;
        new Accessor<TextDropdown, string[]>("StartingItems").Set(
            dropdown,
            [.. new StaticAccessor<Dictionary<string, Mesh>>(typeof(Meshes), "GameMeshes").Get().Keys]
        );

        var input = RecoloredTextField()
                .setPlaceholderText("0")
                .configure(tmp => tmp.contentType = TMP_InputField.ContentType.DecimalNumber)
                .fixedSize(70, 0);
        var vector3Editor = new GameObject("Vector 3 Editor", [typeof(RectTransform)]);

        vector3Editor.SetActive(false);
        new SimpleWrapper(vector3Editor)
            .layoutHorizontalInner()
            .add(new TextFieldWrapper(input.gameObject))
            .add(new TextFieldWrapper(input.gameObject.clone()))
            .add(new TextFieldWrapper(input.gameObject.clone()));
        vector3Editor.SetActive(true);

        static GameObject WordlessToggle() => WS.toggle.gameObject.transform.GetChild(0).gameObject;

        var result = new GameObject("Block Settings", [typeof(RectTransform)]);
        result.SetActive(false);
        new SimpleWrapper(result)
            .layoutGrowElementHorizontal()
            .addAndConfigure<Rectangle>(rect =>
            {
                // rect.ShapeProperties.DrawFill = false;
                rect.ShapeProperties.DrawOutline = true;
                var pal = rect.AddComponent<PaletteGraphic>();
                TargetAccess.Set(pal, rect);
                pal.SetPaletteColor(PaletteColor.Secondary);
                var rectPal = rect.AddComponent<PaletteRectangleOutline>();
                RectTargetAccess.Set(rectPal, rect);
                rectPal.SetPaletteColor(PaletteColor.Tertiary);
            })
            .addContainer("Body", container => container
                .layoutVerticalInner()
                .addContainer("Vectors", container => container
                    .layoutHorizontalInner()
                    .addContainer("Names", container => container
                        .layoutVerticalInner()
                        .add(WS.textLine.setLocalizationKey("Position:").setFontSize(30))
                        .add(WS.textLine.setLocalizationKey("Scale:").setFontSize(30))
                        .add(WS.textLine.setLocalizationKey("Rotation:").setFontSize(30))
                    )
                    .addContainer("Values", container => container
                        .layoutVerticalInner()
                        .add(vector3Editor)
                        .add(vector3Editor.clone())
                        .add(vector3Editor.clone())
                    )
                )
                .addContainer("Row 1", container => container
                    // .layoutGrowElementHorizontalInner(elementIndex:  IndexHelper.First)
                    .layoutHorizontalInner()
                    .add(WS.textLine.setLocalizationKey("Outline").setFontSize(30))
                    .add(new SimpleWrapper(WordlessToggle()).fixedSize(70, 40))
                    .add(WS.textLine.setLocalizationKey("Collision").setFontSize(30))
                    .add(new SimpleWrapper(WordlessToggle()).fixedSize(70, 40))
                )
                .addContainer("Row 2", container => container
                    .layoutGrowElementHorizontalInner(elementIndex: IndexHelper.First)
                    .add(new SimpleWrapper(dropdown.gameObject).fixedSize(270, 40))
                    .add(WS.colorPicker.fixedSize(140, 40))
                )
            )
            .addContainer("Movement", container => container
                .layoutVerticalInner()
                // this would be an up, down, and delete buttons to deal with rearranging blocks.
                // .add(WS.button)
            )
        ;
        result.SetActive(true);
        return result;
    }

    private static Accessor<PaletteRectangleOutline, Rectangle> RectTargetAccess = new("Target");
    private static Accessor<PaletteGraphic, Graphic> TargetAccess = new("Target");
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

    public void Initialize()
    {
        // grab references to all the important things above
    }
}

