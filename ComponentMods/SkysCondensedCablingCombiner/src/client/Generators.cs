using System.Linq;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;

namespace SkysCondensedCablingCombiner.Client;

public class CombinerPlacementRulesGenerator : DynamicPlacingRulesGenerator<int>
{
    public override PlacingRules GeneratePlacingRulesFor(int inputCount)
    {
        var width = (inputCount + 1) / 2;
        return new()
        {
            OffsetDimensions = new(width, 1),
            AllowFineRotation = false,
            CanBeFlipped = true,
            FlippingPointHeight = 0.5f,
            SecondaryGridPositions = [new(0, 0.5f), new(0.5f, 0.5f), new(1, 0.5f)],
            GridPositionsAreRelative = true,
        };
    }

    public override int GetIdentifierFor(ComponentData componentData) => componentData.InputCount - 1;
}

public class CombinerPrefabGenerator : DynamicPrefabGenerator<int>
{
    public static readonly Color24 BlockColor = new(0x1a9cc7);
    public override int GetIdentifierFor(ComponentData componentData) => componentData.InputCount - 1;
    public override (int inputCount, int outputCount) GetDefaultPegCounts() => (inputCount: 3, outputCount: 0);

    public override Prefab GeneratePrefabFor(int inputCount)
    {
        var width = (inputCount + 1) / 2;
        var offset = (inputCount & 1) == 1 ? 0 : -0.25f;
        return new()
        {
            Blocks = [
                new() {
                    RawColor = BlockColor,
                    Scale = new(width - 1/4f, 1, 1),
                    Position = new(width / 2f - 3 / 8f, 0, 0),
                },
                new() {
                    MeshName = "BufferBody",
                    Rotation = new(0, -90, -90),
                    RawColor = BlockColor,
                    Scale = new(1, 2 / 3f, 1 / 4f),
                    Position = new(-3 / 8f, 0.5f, -0.5f),
                },
            ],
            Inputs = [
                new()
                {
                    Position = new((width - 1) / 2f, 0.5f, 0.5f),
                    Rotation = new(90, 0, 0),
                    Length = 0.62f,
                },
                .. Enumerable.Range(0, inputCount).Select(i =>
                    new ComponentInput()
                    {
                        Position = new(i / 2f  + offset, 0.5f, -0.5f),
                        Rotation = new(-90, 0, 0),
                        Length = 0.62f,
                    }
                ),
            ]
        };
    }
}
