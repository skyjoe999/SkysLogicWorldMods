using LogicAPI.Server.Components;
using SkysGeneralLib.Server;
using SkysGeneralLib.Server.TypeExtensions;

namespace SkysCompactCircuits.Server;

public class ExportBuffer : LogicComponent
{
    // We are relying on the idea that components are added to their parents in order
    // and that an export inside of a packed circuit will never change parents.
    private IOutputPeg CircuitPeg;

    protected override void Initialize()
    {
        var packedCircuitType = Services.ComponentTypesManager.GetComponentType("SkysCompactCircuits.PackedCircuit");
        var parent = Component.Data.Parent;
        var component = parent.GetComponent();
        while (parent.IsNotEmpty() && component.Data.Type != packedCircuitType)
            component = (parent = component.Parent).GetComponent();

        if (!parent.IsEmpty() && parent.GetLogicComponent() is PackedCircuit circuit)
        {
            CircuitPeg = circuit.GetNextExportOutput();
        }
    }

    protected override void DoLogicUpdate()
    {
        var on = Outputs[0].On = Inputs[0].On;
        if(CircuitPeg is not null)
            CircuitPeg.On = on;
    }
}
