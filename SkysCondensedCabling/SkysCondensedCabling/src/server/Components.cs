using LogicAPI.Server.Components;

namespace SkysCondensedCabling.Server;

class Peg2 : LogicComponent, IHasSuperPegs
{
    public int InputSuperSize(int index) => 4;
    public int OutputSuperSize(int index) => 0;
}

class Combiner : LogicComponent, IHasSuperPegs
{
    public int InputSuperSize(int index) => index == 0 ? 4 : 0;
    public int OutputSuperSize(int index) => 0;
    protected override void Initialize()
    {
        var super = (SuperInputPeg)Inputs[0];
        for (int i = 0; i < Inputs.Count - 1; i++)
            super.AddPhasicLinkWith(Inputs[i + 1], i);
    }
    public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex) => false;
}


class SuperAndGate : LogicComponent, IHasSuperPegs
{
    public int InputSuperSize(int index) => 4;
    public int OutputSuperSize(int index) => 4;
    SuperOutputPeg Output;
    SuperInputPeg Input1;
    SuperInputPeg Input2;
    protected override void Initialize()
    {
        Output = (SuperOutputPeg)Outputs[0];
        Input1 = (SuperInputPeg)Inputs[0];
        Input2 = (SuperInputPeg)Inputs[1];
    }

    protected override void DoLogicUpdate()
    {
        for (var index = 0; index < 4; index++)
            Output[index] = Input1[index] && Input2[index];
    }
}

class SuperFastBuffer : LogicComponent, IHasSuperPegs
{
    public int InputSuperSize(int index) => 4;
    public int OutputSuperSize(int index) => 4;

    protected override void Initialize()
    {
        Inputs[0].AddOneWayPhasicLinkTo(Inputs[1]);
    }

    public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex)
    {
        return false;
    }
}
