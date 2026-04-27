using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EccsLogicWorldAPI.Shared.AccessHelper;
using HarmonyLib;
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Server;
using LogicAPI.Server.Components;
using LogicWorld.Server;
using LogicWorld.Server.Circuitry;
using SkysGeneralLib.Server;
using SkysGeneralLib.Shared.AccessTools;

namespace SkysCondensedCabling.Server;

public class SkysCondensedCabling_ServerMod : ServerMod
{
    protected override void Initialize()
    {
        var harmony = new Harmony("SkysCondensedCablingServer");
        // Harmony.DEBUG = true;
        harmony.PatchAll();
    }
}


public class SuperOutputPeg(OutputPeg output, int size) : IOutputPeg
{
    public readonly OutputPeg Peg = output;
    public bool On { get => Peg.On; set => Peg.On = value; }
    public PegAddress Address => Peg.Address;

    private readonly bool[] Data = new bool[size];
    public int Size => Data.Length;

    // Ignores indexing past end of Data
    public bool this[int index]
    {
        get => index < Size && Data[index];
        set
        {
            if (index >= Size || Data[index] == value) return;
            Data[index] = value;
            Peg.On = Data.Any(b => b);
            foreach (var updatable in ConnectedUpdatables)
                updatable.QueueLogicUpdate();
        }
    }

    private readonly List<ILogicUpdatable> ConnectedUpdatables = ConnectedUpdatablesAccess.Get(output);
    private static readonly Accessor<OutputPeg, List<ILogicUpdatable>> ConnectedUpdatablesAccess = new("ConnectedUpdatables");
}

// Only exists within logic components (not the circuit manager and such)
// Can safely wire to and secret link with same type
public partial class SuperInputPeg(InputPeg input) : IInputPeg
{
    public readonly InputPeg Peg = input ?? throw new ArgumentNullException();
    public SuperCluster Cluster;

    public PegAddress Address => Peg.Address;
    public bool On => Peg.On;
    public bool this[int index] => Cluster[index];

    public void AddSecretLinkWith(IInputPeg otherInput) =>
        Peg.AddSecretLinkWith((otherInput as SuperInputPeg)?.Peg ?? throw new ArgumentException("Cannot link super pegs and regular pegs"));
    public void RemoveSecretLinkWith(IInputPeg otherInput) =>
        Peg.RemoveSecretLinkWith((otherInput as SuperInputPeg)?.Peg ?? throw new ArgumentException("Cannot link super pegs and regular pegs"));
    public void RemoveAllSecretLinks() => Peg.RemoveAllSecretLinks();
}

interface IHasSuperPegs
{
    int InputSuperSize(int index);
    int OutputSuperSize(int index);
}
#region Patches
[HarmonyPatch(typeof(ClusterFactory), nameof(ClusterFactory.Create))]
class ClusterFactoryCreateOverride
{
    static void Postfix(ClusterFactory __instance, ref Cluster __result, InputPeg[] inputs, OutputPeg[] outputs)
    {
        if (SuperClusterFactory.IsMainWorld(__instance))
            __result = SuperClusterFactory.Create(__result, inputs, outputs);
    }
}

[HarmonyPatch(typeof(ClusterFactory), nameof(ClusterFactory.CreateStarter))]
class ClusterFactoryCreateStarterOverride
{
    static void Postfix(ClusterFactory __instance, ref Cluster __result, InputPeg input)
    {
        if (SuperClusterFactory.IsMainWorld(__instance))
            __result = SuperClusterFactory.CreateStarter(__result, input);
    }
}

[HarmonyPatch(typeof(CircuitryManager), "InitializePegsInCircuitModel")]
class PegInitializationOverride
{
    static void Postfix(LogicComponent logic)
    {
        if (logic is not IHasSuperPegs super) return;
        var inputs = _InputsAccess.Get(logic);
        foreach ((var p, var i) in inputs.Select((p, i) => (p, i)))
            if (super.InputSuperSize(p.Address.PegIndex) > 0 && p is InputPeg _p)
            {
                inputs[i] = new SuperInputPeg(_p);
                ((SuperInputPeg)inputs[i]).Cluster = ClusterAccess.Get(_p) as SuperCluster;
            }

        var outputs = _OutputsAccess.Get(logic);
        foreach ((var p, var i) in outputs.Select((p, i) => (p, i)))
            if (super.OutputSuperSize(p.Address.PegIndex) > 0 && p is OutputPeg _p)
                outputs[i] = new SuperOutputPeg(_p, super.OutputSuperSize(p.Address.PegIndex));
    }

    private static readonly Accessor<LogicComponent, IInputPeg[]> _InputsAccess = new("_Inputs");
    private static readonly Accessor<LogicComponent, IOutputPeg[]> _OutputsAccess = new("_Outputs");
    private static readonly Accessor<InputPeg, Cluster> ClusterAccess = new("Cluster");
}


[HarmonyPatch(typeof(LogicManager), nameof(LogicManager.DoLogicUpdate))]
class LogicUpdateSuperFinal
{
    static void Postfix() => SuperCluster.FinalUpdater.UpdateContainer.UpdateAll();
}
[HarmonyPatch(typeof(CircuitryManager), nameof(CircuitryManager.FinalizeBatchClusterInitialization))]
class DummyInputManagerRunner
{
    static void Postfix() => DummyInputManager.FixAll();
}

#endregion

static class DummyInputManager
{
    private static readonly List<(InputPeg other, SuperInputPeg super, int channel)> DummiesWith = [];
    private static readonly List<(InputPeg other, SuperInputPeg super, int channel)> DummiesTo = [];
    private static readonly List<(InputPeg other, SuperInputPeg super, int channel)> DummiesFrom = [];
    public static void FixAll()
    {
        foreach ((var other, var super, var channel) in DummiesWith)
            super.AddPhasicLinkWithUnsafe(other, channel);
        foreach ((var other, var super, var channel) in DummiesTo)
            super.AddOneWayPhasicLinkTo(other, channel);
        foreach ((var other, var super, var channel) in DummiesFrom)
            super.AddOneWayPhasicLinkFrom(other, channel);
        DummiesWith.Clear();
        DummiesTo.Clear();
        DummiesFrom.Clear();
    }
    public static void MakeDummyTwoWay(SuperInputPeg super, int channel, InputPeg other) => DummiesWith.Add((other, super, channel));
    public static void MakeDummyFrom(SuperInputPeg super, int channel, InputPeg other) => DummiesFrom.Add((other, super, channel));
    public static void MakeDummyTo(SuperInputPeg super, int channel, InputPeg other) => DummiesTo.Add((other, super, channel));
    
}

public class SuperCluster : Cluster
{
    public int Size => States.Size;
    public readonly List<SuperOutputPeg> ConnectedSuperOutputs;
    private readonly ClusterStates States;


    public bool this[int index] => index < Size && States[index];

    public SuperCluster(Cluster cluster) : base(
        cluster.StateID,
        CircuitStatesAccess.Get(cluster),
        ContainerAccess.Get(cluster),
        cluster.ConnectedInputs,
        cluster.ConnectedOutputs,
        cluster.ConnectedUpdatables
    )
    {
        ConnectedSuperOutputs = [.. cluster.ConnectedOutputs.Select(p => p.GetSuperPeg()).Where(p => p is not null)];
        var size = ConnectedSuperOutputs.Select(p => p.Size).Concat(ConnectedInputs.Select(p => p.GetSuperSize())).Max();
        Final = new(this);
        States = new(size > 0 ? size : 0, this);
        Final.LogicUpdate();
    }


    public override void LogicUpdate()
    {
        // This will trigger any relevant partial clusters to update just like a normal cluster
        for (int index = 0; index < Size; index++)
            States.Outputs[index].On = ConnectedSuperOutputs.Any(p => p[index]);
    }

    public override void Destroy()
    {
        foreach (var cluster in States.PartialClusters)
            cluster.Destroy();
        foreach (var input in ConnectedInputs)
            if (input.GetSuperPeg() is SuperInputPeg peg && peg.Cluster == this) peg.Cluster = null;
        base.Destroy();
    }
    public void InitializeInnerClusterStatesAfterLinking()
    {
        for (int index = 0; index < Size; index++)
            States.PartialClusters[index].SetOnState(ConnectedSuperOutputs.Any(p => p[index]) || States.PartialClusters[index].AnyLinkedClustersOn());
    }

    private static readonly Accessor<Cluster, CircuitStates> CircuitStatesAccess = new("CircuitStates");
    private static readonly Accessor<SelfUpdating<Cluster>, SelfUpdatingContainer<Cluster>> ContainerAccess = new("Container");

    #region Linking
    private IEnumerable<(Cluster, Cluster)> GetPairs(SuperCluster other) => States.PartialClusters.Zip(other.States.PartialClusters, (a, b) => (a, b));
    public void AddSuperTwoWayLinkWith(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.AddTwoWayLinkWith(p.Item2));
    public void RemoveSuperTwoWayLinkWith(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.RemoveTwoWayLinkWith(p.Item2));
    public void AddSuperOneWayLinkTo(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.AddOneWayLinkTo(p.Item2));
    public void RemoveSuperOneWayLinkTo(SuperCluster other) => GetPairs(other).ForEach(p => p.Item1.RemoveOneWayLinkTo(p.Item2));

    public InputPeg GetChannel(int channel) => States.Inputs[channel];
    public void AddTwoWayLinkWith(Cluster other, int channel) => States.PartialClusters[channel].AddTwoWayLinkWith(other);
    public void RemoveTwoWayLinkWith(Cluster other, int channel) => States.PartialClusters[channel].RemoveTwoWayLinkWith(other);
    public void AddOneWayLinkTo(Cluster other, int channel) => States.PartialClusters[channel].AddOneWayLinkTo(other);
    public void RemoveOneWayLinkTo(Cluster other, int channel) => States.PartialClusters[channel].RemoveOneWayLinkTo(other);
    public void AddOneWayLinkFrom(Cluster other, int channel) => other.AddOneWayLinkTo(States.PartialClusters[channel]);
    public void RemoveOneWayLinkFrom(Cluster other, int channel) => other.RemoveOneWayLinkTo(States.PartialClusters[channel]);
    #endregion

    public FinalUpdater Final; // Probably not critical? Mostly just updates the color
    public class FinalUpdater(SuperCluster Cluster) : SelfUpdating<FinalUpdater>(UpdateContainer)
    {
        public override void LogicUpdate() => Cluster.SetOnState(Cluster.States.Any());
        public static readonly SelfUpdatingContainer<FinalUpdater> UpdateContainer = new();
    }
}

public class ClusterStates : CircuitStates
{
    public readonly int Size;
    public readonly OutputPeg[] Outputs;
    public readonly InputPeg[] Inputs;
    public readonly Cluster[] PartialClusters;
    public ClusterStates(int size, SuperCluster cluster) : base()
    {
        Size = size;
        StatesAccess.SetValue(this, NewStateArray.Invoke([Size * 2]));
        // Ensures the pegs have ids [size, size * 2 - 1]
        var addresses = new Stack<int>();
        for (int i = 0; i < size; i++)
            addresses.Push(size + size - i - 1);
        _UnusedAddressesAccess.Set(this, addresses);
        Outputs = new OutputPeg[size];
        for (int i = 0; i < size; i++)
            Outputs[i] = NewOutputPeg.Invoke([new OutputAddress(new(), i), false, this]) as OutputPeg;
        Inputs = new InputPeg[size];
        for (int i = 0; i < size; i++)
            Inputs[i] = NewInputPeg.Invoke([new InputAddress(new(), i), null, false, null]) as InputPeg;

        var container = ContainerAccess.Get(cluster);
        var updatables = cluster.ConnectedUpdatables.Concat([cluster.Final]).ToArray();
        PartialClusters = new Cluster[size];
        for (int i = 0; i < size; i++)
        {
            PartialClusters[i] = new Cluster(i, this, container, [Inputs[i]], [Outputs[i]], updatables);
            PartialClusters[i].QueueLogicUpdate();
            OutputConnectedUpdatablesAccess.Set(Outputs[i], [PartialClusters[i]]);
        }
        for (int i = 0; i < size; i++)
            Inputs[i].SetCluster(PartialClusters[i]);
    }

    public bool Any() => Enumerable.Range(0, Size).Any(i => this[i]);

    // I love private and internal things SO MUCH!!!
    private static readonly Accessor<OutputPeg, List<ILogicUpdatable>> OutputConnectedUpdatablesAccess = new("ConnectedUpdatables");
    private static readonly Accessor<CircuitStates, Stack<int>> _UnusedAddressesAccess = new("_UnusedAddresses");
    private static readonly ConstructorInfo NewOutputPeg = typeof(OutputPeg).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(OutputAddress), typeof(bool), typeof(CircuitStates)]);
    private static readonly ConstructorInfo NewInputPeg = typeof(InputPeg).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(InputAddress), typeof(LogicComponent), typeof(bool), typeof(ICircuitryManager)]);
    private static readonly ConstructorInfo NewStateArray = typeof(CircuitStates).GetNestedType("State", BindingFlags.NonPublic).MakeArrayType().GetConstructor([typeof(int)]);
    private static readonly FieldInfo StatesAccess = Fields.get(typeof(CircuitStates), "States");
    private static readonly Accessor<SelfUpdating<Cluster>, SelfUpdatingContainer<Cluster>> ContainerAccess = new("Container");
}



public static class PegExtension
{
    public static int GetSuperSize(this InputPeg peg) => peg.LogicComponent is not IHasSuperPegs superComp ? 0 : superComp.InputSuperSize(peg.iAddress.PegIndex);
    public static int GetSuperSize(this OutputPeg peg) => Services.ICircuitryManager.LookupComponent(peg.Address.ComponentAddress) is not IHasSuperPegs superComp ? 0 : superComp.OutputSuperSize(peg.oAddress.PegIndex);
    public static SuperOutputPeg GetSuperPeg(this OutputPeg peg) =>
        Services.ICircuitryManager.LookupComponent(peg.Address.ComponentAddress)?.Outputs[peg.Address.PegIndex] as SuperOutputPeg;
    public static SuperInputPeg GetSuperPeg(this InputPeg peg) =>
        peg.LogicComponent?.Inputs[peg.Address.PegIndex] as SuperInputPeg;

    public static Cluster GetCluster(this InputPeg peg) => Cluster.Get(peg);
    public static void SetCluster(this InputPeg peg, Cluster cluster) => Cluster.Set(peg, cluster);

    private static readonly Accessor<InputPeg, Cluster> Cluster = new("Cluster");
}
