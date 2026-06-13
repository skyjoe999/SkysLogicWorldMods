using System;
using System.Collections.Generic;
using LogicAPI.Data;
using LogicWorld.Interfaces;
using SkysCompactCircuits.Client.ClientCode;
using UnityEngine;

namespace SkysCompactCircuits.Client.Addons;

public abstract class ClientAddon
{
    public PackedCircuit Parent;
    public ComponentAddress Reference { get; private set; }
    public IReadOnlyList<IRenderedEntity> BlockEntities { get; private set; }

    public virtual IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder) => []; // If you need to reference them later, save them locally
    protected virtual void Initialize() { }
    public virtual void OnComponentDestroyed() { }

    public void SetReference(
        ComponentAddress reference,
        IReadOnlyList<IRenderedEntity> blockEntities
    )
    {
        if (BlockEntities is not null)
            throw new Exception("Reference can only be set once");
        Reference = reference;
        BlockEntities = blockEntities;
        Initialize();
    }
}
