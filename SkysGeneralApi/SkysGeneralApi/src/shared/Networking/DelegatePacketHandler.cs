using System;
using System.Net;
using LogicAPI.Networking;
using LogicWorld.SharedCode.Networking;
#if LW_SIDE_SERVER
using EccsLogicWorldAPI.Server.Injectors;
#else
using EccsLogicWorldAPI.Client.Injectors;
#endif

namespace SkysGeneralLib.Shared.Networking;

public class FuncPacketHandler<T>(Action<T, Connection, IPEndPoint> handle) : PacketHandler<T>
{
    // Stupid ref struct restrictions!
    public FuncPacketHandler(Action<T, Connection> handle) : this((packet, sender, _) => handle.Invoke(packet, sender)) {}
    public FuncPacketHandler(Action<T, IPEndPoint> handle) : this((packet, _, senderEndPoint) => handle.Invoke(packet, senderEndPoint)) {}
    public FuncPacketHandler(Action<T> handle) : this((packet, _, _) => handle.Invoke(packet)) {}

    public override void Handle(T packet, HandlerContext context) => handle.Invoke(packet, context.Sender, context.SenderEndPoint);
    public static FuncPacketHandler<T> Add(Action<T, Connection> handle) => Add((packet, sender, _) => handle.Invoke(packet, sender));
    public static FuncPacketHandler<T> Add(Action<T, IPEndPoint> handle) => Add((packet, _, senderEndPoint) => handle.Invoke(packet, senderEndPoint));
    public static FuncPacketHandler<T> Add(Action<T> handle) => Add((packet, _, _) => handle.Invoke(packet));
    public static FuncPacketHandler<T> Add(Action<T, Connection, IPEndPoint> handle)
    {
        var handler = new FuncPacketHandler<T>(handle);
        RawPacketHandlerInjector.addPacketHandler(handler);
        return handler;
    }
}
