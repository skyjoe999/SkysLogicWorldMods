using System.Linq;
using LogicWorld.Server.Circuitry;

namespace SkysCondensedCablingLib.Server;

// Make sure to free the circuit state first!
public class SuperOutputPeg(OutputPeg output, int size, SuperPegFamily family) : OutputPeg(output.oAddress, output.On, output.CircuitStates)
{
    private readonly bool[] Data = new bool[size];

    public int Size => Data.Length;
    public readonly SuperPegFamily Family = family;

    // Ignores indexing past end of Data
    public bool this[int index]
    {
        get => index < Size && Data[index];
        set
        {
            if (index >= Size || Data[index] == value) return;
            Data[index] = value;
            On = Data.Any(b => b);
            foreach (var updatable in ConnectedUpdatables)
                updatable.QueueLogicUpdate();
        }
    }
}
