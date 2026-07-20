using System;
using System.Linq;
using LogicAPI.Server;
using LogicWorld.Server.Circuitry;
using SkysCondensedCablingCombiner.Shared;
using SkysCondensedCablingLib.Server;

namespace SkysCondensedCablingCombiner.Server;

public class SkysCondensedCablingCombiner_ServerMod : ServerMod;

public class Combiner : LogicComponent<ICombinerData>, IHasSuperPegs
{
    public int InputSuperSize(int index) => index == 0 ? Math.Max(1, Data.BitsPerInput * (Inputs.Count - 1)) : Data.BitsPerInput > 1 ? Data.BitsPerInput : 0;
    public int OutputSuperSize(int index) => 0;

    public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex) => false;

    public override void SetDataDefaultValues() => Data.Initialize();

    public (int, int) PreviousData = default;
    public override void OnCustomDataUpdated()
    {
        if (PreviousData == (Inputs.Count - 1, Data.BitsPerInput))
            return;

        var maxBPI = (int)MathF.Floor(256f / (Inputs.Count - 1));
        if (Data.BitsPerInput > maxBPI)
        {
            Data.BitsPerInput = maxBPI;
            return;
        }

        // Call this any time InputSuperSize changes
        this.EnsureSuperPegsAreCorrect();

        PreviousData = (Inputs.Count - 1, Data.BitsPerInput);

        var primary = Inputs[0] as SuperInputPeg; // Should never be null but I fear my own code...
        foreach (var (other, channel) in primary?.PartialPhasicLinks.ToList() ?? [])
            primary.RemovePhasicLinkWith(other, channel);

        for (int input = 0; input < Inputs.Count - 1; input++)
            for (int bit = 0; bit < Data.BitsPerInput; bit++)
                primary?.AddPhasicLinkWith(Inputs[input + 1], (bit + input * Data.BitsPerInput, bit));
    }
}
