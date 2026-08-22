using System.Collections.Generic;
using DG.Tweening;
using FancyInput;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Audio;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Chunks;
using LogicWorld.SharedCode.ComponentCustomData;
using LogicWorld.SharedCode.Components;
using SkysGeneralLib.Shared.AccessTools;
using TMPro;
using UnityEngine;

namespace SkysCompactCircuits.Client.Addons;

[HarmonyPatch]
public class KeyAddon(IKeyData Data, Vector3 RawBlockScale) : SuperAddon<KeyAddon.SafeKey>
{
    protected override void Initialize()
    {
        base.Initialize();
        WorldPositionRotationCalculatedAccess.Set((ComponentDataManager)Inner.Component, true);
        WorldPositionAccess.Set((ComponentDataManager)Inner.Component, Inner.Transform.position - new Vector3(0, RawBlockScale.y, 0));
        SafeKey.DataUpdateInner(Inner);

        Inner.WorldUpPosition = Inner.Transform.position;
        var transform = Inner.Decorations[1].DecorationObject.GetComponent<BoxCollider>().transform;
        Inner.WorldDownPosition = transform.position;
        transform.position = Inner.WorldUpPosition;

        Inner.Decorations[0].DecorationObject.GetComponentInChildren<VisibilityDetector>().OnBecomeVisible += Inner.QueueFrameUpdate;

        HasBeenFullyInitializedAccess.Set(Inner, true);
        Inner.QueueFrameUpdate();
    }

    public override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
    {
        Inner = new();
        DummyEntity.Scale = RawBlockScale;
        BlockEntitiesAccess.Set(Inner, [DummyEntity]);
        var decorations = Inner.GenerateDecorations(parentToCreateDecorationsUnder);
        DecorationsAccess.Set(Inner, decorations);

        decorations[0].DecorationObject.GetComponentInChildren<MeshRenderer>().material = WorldRendererAccess.Get(Parent).MaterialsSource.SolidColor(Data.KeyColor);

        var keyLabel = decorations[0].DecorationObject.GetComponentInChildren<TextMeshPro>();
        keyLabel.text = ((RawInput)Data.BoundInput).DisplayName();
        keyLabel.color = Data.KeyLabelColor.WithAlphaChannel();

        return decorations;
    }

    [HarmonyPatch]
    public class SafeKey() : Key
    {
        public bool previouslyDown;
        public Vector3 WorldUpPosition;
        public Vector3 WorldDownPosition;
        public Transform Transform;

        public new IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
        {
            var decorations = base.GenerateDecorations(parentToCreateDecorationsUnder);

            var offset = new Vector3(0, -GetRawBlockScale().y, 0);
            decorations[0].LocalPosition += offset;
            decorations[1].LocalPosition += new Vector3(0f, -0.045f, 0f) + offset;
            Transform = decorations[0].DecorationObject.transform;
            return decorations;
        }

        #region Harmony
        [HarmonyReversePatch][HarmonyPatch(typeof(Key), "DataUpdate")] public static void DataUpdateInner(SafeKey component) { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ReplaceWithNew(instructions, "DoClientSideKeyStateUpdates"); _ = Transpiler(null); }
        [HarmonyReversePatch][HarmonyPatch(typeof(Key), "WeArePressingThis")] public static void WeArePressingThisInner(SafeKey component, bool areWePressingThis) { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ReplaceWithNew(instructions, "DoClientSideKeyStateUpdates"); _ = Transpiler(null); }
        [HarmonyReversePatch][HarmonyPatch(typeof(Key), "FrameUpdate")] public static void FrameUpdateInner(SafeKey component) { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ReplaceWithNew(instructions, "WeArePressingThis"); _ = Transpiler(null); }
        public void WeArePressingThis(bool areWePressingThis) => WeArePressingThisInner(this, areWePressingThis);

        private static IEnumerable<CodeInstruction> ReplaceWithNew(IEnumerable<CodeInstruction> instructions, string methodName)
        {
            var oldMethod = typeof(Key).Method(methodName);
            var newMethod = typeof(SafeKey).Method(methodName);
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(oldMethod))
                    instruction.operand = newMethod;
                yield return instruction;
            }
        }
        #endregion

        protected override void DataUpdate() => DataUpdateInner(this);
        protected override void FrameUpdate() => FrameUpdateInner(this);

        public void DoClientSideKeyStateUpdates()
        {
            if (PlacedInMainWorld && previouslyDown != Data.KeyDown)
            {
                if (HasBeenFullyInitialized)
                {
                    SoundPlayer.PlaySoundAt(Data.KeyDown ? Sounds.KeyDown : Sounds.KeyUp, WorldDownPosition);
                    ShortcutExtensions.DOKill(Transform, false);
                    ShortcutExtensions.DOLocalMove(Transform, Data.KeyDown ? WorldDownPosition : WorldUpPosition, 0.04f, false);
                }
                else
                    Transform.localPosition = Data.KeyDown ? WorldDownPosition : WorldUpPosition;

                previouslyDown = Data.KeyDown;
            }
        }
    }


    private static readonly RenderedEntity DummyEntity = typeof(RenderedEntity).Constructor().Invoke(null) as RenderedEntity;
    private static readonly Accessor<ComponentDataManager, bool> WorldPositionRotationCalculatedAccess = new("WorldPositionRotationCalculated");
    private static readonly Accessor<ComponentDataManager, Vector3> WorldPositionAccess = new("_WorldPosition");
}

public class KeyAddonGenerator : ClientAddonGenerator<IKeyData>
{
    public override ClientAddon GenerateAddon(ComponentData componentData, IKeyData data) => new KeyAddon(data, Vector3.one * 0.3f);
    public override int GetBlockCount(ComponentData componentData) => 0;
    public override Block[] GenerateBlocks(ComponentData componentData, IKeyData data) => [];
}
