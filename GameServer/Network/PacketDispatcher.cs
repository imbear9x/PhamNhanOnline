using GameServer.Network.Handlers;
using GameServer.Network.Interface;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Network;

public sealed class PacketDispatcher
{
    private readonly IServiceProvider _provider;
    private readonly IEnumerable<IPacketMiddleware> _middlewares;
    public PacketDispatcher(IServiceProvider provider, IEnumerable<IPacketMiddleware> middlewares)
    {
        _provider = provider;
        _middlewares= middlewares;
    }

    public async Task DispatchAsync(ConnectionSession session, IPacket packet)
    {
        var enumerator = _middlewares.GetEnumerator();

        Task Next()
        {
            if (!enumerator.MoveNext())
                return ExecuteHandler(session, packet);

            return enumerator.Current.InvokeAsync(session, packet, Next);
        }

        await Next();
    }
    private Task ExecuteHandler(ConnectionSession session, IPacket packet)
    {
        // ham next() ben tren se goi vao day
        return DispatchDynamic(session, (dynamic)packet);
    }
    private Task DispatchDynamic<TPacket>(ConnectionSession session, TPacket packet)
    {
        return DispatchAsyncInternal(session, packet);
    }

    private async Task DispatchAsyncInternal<TPacket>(ConnectionSession session, TPacket packet)
    {
        var handler = _provider.GetRequiredService<IPacketHandler<TPacket>>();
        await handler.HandleAsync(session, packet);
    }

}

