using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicWorld.Rendering.Components;

namespace SkysSockets.Client.ClientCode;

public class MultiSocket : ComponentClientCode<MultiSocket.IData>, IVirtualSocketHolder
{
    public interface IData; // Only used for menu identification

    protected override void SetDataDefaultValues()
    {
    }

    void IVirtualSocketHolder.SetBlockColor(GpuColor color, int blockIndex) => base.SetBlockColor(color, blockIndex);
    protected override void FrameUpdate() => ((IVirtualSocketHolder) this).frameUpdate();

    public IEnumerable<int> BlockIndicies => Enumerable.Range(0, InputCount);
    public IEnumerable<int> InputIndicies => Enumerable.Range(0, InputCount);
}
