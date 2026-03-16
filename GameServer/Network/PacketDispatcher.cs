using GameServer.Network.Handlers;
using GameServer.Network.Interface;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Network;

public sealed class PacketDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEnumerable<IPacketMiddleware> _middlewares;

    public PacketDispatcher(IServiceScopeFactory scopeFactory, IEnumerable<IPacketMiddleware> middlewares)
    {
        _scopeFactory = scopeFactory;
        _middlewares = middlewares;
    }

    public async Task DispatchAsync(ConnectionSession session, IPacket packet)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var scopedProvider = scope.ServiceProvider;
        var enumerator = _middlewares.GetEnumerator();

        Task Next()
        {
            if (!enumerator.MoveNext())
                return ExecuteHandler(scopedProvider, session, packet);

            return enumerator.Current.InvokeAsync(session, packet, Next);
        }

        await Next();
    }

    private static Task ExecuteHandler(IServiceProvider serviceProvider, ConnectionSession session, IPacket packet)
    {
        return DispatchDynamic(serviceProvider, session, (dynamic)packet);
    }

    private static Task DispatchDynamic<TPacket>(IServiceProvider serviceProvider, ConnectionSession session, TPacket packet)
    {
        return DispatchAsyncInternal(serviceProvider, session, packet);
    }

    private static async Task DispatchAsyncInternal<TPacket>(IServiceProvider serviceProvider, ConnectionSession session, TPacket packet)
    {
        var handler = serviceProvider.GetRequiredService<IPacketHandler<TPacket>>();
        await handler.HandleAsync(session, packet);
    }
}
