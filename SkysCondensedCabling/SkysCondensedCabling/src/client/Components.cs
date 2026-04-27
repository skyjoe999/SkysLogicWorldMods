
using LogicWorld.Rendering.Components;

namespace SkysCondensedCabling.Client;

class AllSuper : ComponentClientCode, IHasSuperPegs
{
    public bool IsInputSuper(int index) => true;
    public bool IsOutputSuper(int index) => true;

    protected override void Initialize() => (this as IHasSuperPegs).Register();
    protected override void OnComponentDestroyed() => (this as IHasSuperPegs).Unregister();
}

class Combiner : ComponentClientCode, IHasSuperPegs
{
    public bool IsInputSuper(int index) => index == 0;
    public bool IsOutputSuper(int index) => false;

    protected override void Initialize() => (this as IHasSuperPegs).Register();
    protected override void OnComponentDestroyed() => (this as IHasSuperPegs).Unregister();
}
