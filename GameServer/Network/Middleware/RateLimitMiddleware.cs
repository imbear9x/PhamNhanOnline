using System.Collections.Concurrent;
using GameServer.Network.Interface;
using GameShared.Packets;

namespace GameServer.Network.Middleware;

public sealed class RateLimitMiddleware : IPacketMiddleware
{
    private readonly ConcurrentDictionary<RateLimitKey, long> _lastPacketTicks = new();

    public async Task InvokeAsync(ConnectionSession session, IPacket packet, Func<Task> next)
    {
        var profile = PacketTransportPolicy.Resolve(packet);
        if (profile.MinIntervalMs <= 0)
        {
            await next();
            return;
        }

        var key = new RateLimitKey(session.ConnectionId, packet.GetType());
        var now = Environment.TickCount64;
        if (_lastPacketTicks.TryGetValue(key, out var lastTick) &&
            now - lastTick < profile.MinIntervalMs)
        {
            return;
        }

        _lastPacketTicks[key] = now;
        await next();
    }

    private readonly record struct RateLimitKey(int ConnectionId, Type PacketType);
}
