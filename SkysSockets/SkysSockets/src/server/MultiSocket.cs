using LogicAPI.Server.Components;

namespace SkysSockets.Server.LogicCode;

public class MultiSocket : LogicComponent
{
    private readonly VirtualSocketHolder sockets;
    public MultiSocket() => sockets = new VirtualSocketHolder(this);

    protected override void Initialize() => QueueLogicUpdate();
    protected override void OnCustomDataUpdated() => QueueLogicUpdate();
    public override void OnComponentMoved() => sockets.OnComponentMovedUpdate();

    protected override void DoLogicUpdate()
    {
        if (sockets.HasSockets())
            return;
        sockets.GenerateSockets(
            Inputs,
            Shared.MultiSocket.GetSocketPositions(Inputs.Count).ConvertAll(i => i * 0.3f),
            Shared.MultiSocket.GetSocketRotations(Inputs.Count)
        );

    }

    public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex) => false;
}
