using DG.Tweening;
using HarmonyLib;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Data;
using LogicWorld.Audio;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Chunks;
using LogicWorld.SharedCode.ComponentCustomData;
using LogicWorld.SharedCode.Components;
using UnityEngine;

namespace SkysCompactCircuits.Client.Addons;

public class ButtonAddon<ButtonTypes>(Color24 ButtonColor, Vector3 RawBlockScale) : SuperAddon<ButtonAddon<ButtonTypes>.SafeButton> where ButtonTypes : class, IButtonData
{
    // I could probably refactor this with the experience I have now
    // But this works and isn't even that dumb...
    public class SafeButton() : GenericButton<ButtonTypes>, IPressableButton
    {
        bool? previousDown;
        public Vector3 WorldUpPosition;
        public Vector3 WorldDownPosition;
        public Transform Transform;
        public bool Initialized;
        protected override void DataUpdate()
        {
            VisualButton.material = WorldRenderer.MaterialsSource.SolidColor(Data.ButtonColor);
            if (PlacedInMainWorld && previousDown != Data.ButtonDown)
            {
                if (Initialized)
                {
                    SoundPlayer.PlaySoundAt(Data.ButtonDown ? Sounds.ButtonDown : Sounds.ButtonUp, Address);
                    ShortcutExtensions.DOKill(Transform, false);
                    ShortcutExtensions.DOLocalMove(Transform, Data.ButtonDown ? WorldDownPosition : WorldUpPosition, 0.04f, false);
                }
                else
                    Transform.localPosition = Data.ButtonDown ? WorldDownPosition : WorldUpPosition;
                previousDown = Data.ButtonDown;
            }
        }
        public IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder, IWorldRenderer worldRenderer, Color24 buttonColor)
        {
            var decorations = base.GenerateDecorations(parentToCreateDecorationsUnder);
            LConsole.WriteLine(GetRawBlockScale());
            // PanelButton
            var offset = new Vector3(GetRawBlockScale().x * 0, -GetRawBlockScale().y, GetRawBlockScale().z * 0);
            decorations[0].LocalPosition += offset;
            decorations[1].LocalPosition = DownLocalPosition + offset;
            // decorations[0].LocalPosition = default;
            // decorations[1].LocalPosition = default;
            Transform = decorations[0].DecorationObject.transform;
            VisualButton.material = worldRenderer.MaterialsSource.SolidColor(buttonColor);
            return decorations;
        }
        public new void Initialize()
        {
            WorldUpPosition = Transform.position;
            WorldDownPosition = ButtonShapeCollider.transform.position;
            ButtonShapeCollider.transform.position = WorldUpPosition;

            Initialized = true;
        }
        void IPressableButton.MousePressDown()
        {
            if (previousDown == false)
                Data.ButtonDown = true;
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
        Inner.Initialize();
    }

    public override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        Inner = new();
        DummyEntity.Scale = RawBlockScale;
        BlockEntitiesAccess.Set(Inner, [DummyEntity]);
        return Inner.GenerateDecorations(parentToCreateDecorationsUnder, WorldRendererAccess.Get(Parent), ButtonColor);
    }
    private static readonly RenderedEntity DummyEntity = typeof(RenderedEntity).Constructor().Invoke(null) as RenderedEntity;
}
public class ButtonAddonGenerator : ClientAddonGenerator<IButtonData>
{
    public override ClientAddon GenerateAddon(ComponentData componentData, IButtonData data) => new ButtonAddon<IButtonData>(data.ButtonColor, Vector3.one * 0.3f);
    public override int GetBlockCount(ComponentData componentData) => 0;
    public override Block[] GenerateBlocks(ComponentData componentData, IButtonData data) => [];
}
public class PanelButtonAddonGenerator : ClientAddonGenerator<IPanelButtonData>
{
    public override ClientAddon GenerateAddon(ComponentData componentData, IPanelButtonData data) => new ButtonAddon<IPanelButtonData>(data.ButtonColor, new Vector3(data.SizeX, 1, data.SizeZ) * 0.3f);
    public override int GetBlockCount(ComponentData componentData) => 0;
    public override Block[] GenerateBlocks(ComponentData componentData, IPanelButtonData data) => [];
}
