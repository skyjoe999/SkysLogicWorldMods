using System;
using EccsLogicWorldAPI.Shared.AccessHelper;
using LogicAPI.Data;
using LogicAPI.Server.Components;
using LogicAPI.Services;

namespace SkysGeneralLib.Server.TypeExtensions;

public static class ComponentAddressExtension
{
    public static IComponentInWorld GetComponent(this ComponentAddress address)
        => Services.IWorldData.Lookup(address);

    public static LogicComponent GetLogicComponent(this ComponentAddress address)
        => Services.ICircuitryManager.LookupComponent(address);

    public static bool DescendsFrom(this ComponentAddress address, ComponentAddress other)
    {
        var parent = address;
        while ((parent = parent.GetComponent().Parent) != ComponentAddress.Empty)
            if (parent == other)
                return true;
        return false;
    }

    // Don't really have anywhere else to put this
    public static void UpdateHighestAddedSoFar(this ComponentAddress address)
        => setHighestComponentAddressAddedSoFar(Services.IWorldDataMutator, address.ID);

    private static readonly Action<IWorldDataMutator, uint> setHighestComponentAddressAddedSoFar
        = Delegator.createPropertySetter<IWorldDataMutator, uint>(
            Properties.getPublic(typeof(IWorldDataMutator), "HighestComponentAddressAddedSoFar")
        );
}
