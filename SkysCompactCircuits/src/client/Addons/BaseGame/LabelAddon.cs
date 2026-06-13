// // Couldn't get this to show up, not important enough to get working rn </3
// // The idea was that (non-panel) labels would be able to show up full size
// // With their corner aligned based on the alignment settings
// public class LabelAddonGenerator : ClientAddonGenerator<Label.IData>
// {
//     public override ClientAddon GenerateAddon(ComponentData componentData, Label.IData data) => new LabelAddon(componentData.CustomData);
//     public override int GetBlockCount(ComponentData componentData) => 0;
//     public override Block[] GenerateBlocks(ComponentData componentData, Label.IData data) => [];
// }
// public class LabelAddon(byte[] customData) : ClientAddon
// {
//     public override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
//     {
//         var manager = new CustomDataManager<Label.IData>();
//         manager.TryDeserializeData(customData);
//         var decor = new FloatingText(parentToCreateDecorationsUnder)
//             {
//                 Data =
//                 {
//                     HorizontalAlignment = manager.Data.HorizontalAlignment,
//                     VerticalAlignment = manager.Data.VerticalAlignment,
//                     LabelColor = manager.Data.LabelColor,
//                     LabelFontSizeMax = manager.Data.LabelFontSizeMax,
//                     LabelMonospace = manager.Data.LabelMonospace,
//                     LabelText = manager.Data.LabelText,
//                 },
//                 Scale = new(manager.Data.SizeX, manager.Data.SizeZ),
//                 LocalRotation = Quaternion.identity,
//             };
//             LConsole.WriteLine("added");
//         return
//         [
//             decor
//         ];
//     }
// }
