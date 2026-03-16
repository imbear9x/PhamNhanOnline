using GameServer.Diagnostics;
using GameServer.Network.Interface;
using GameServer.Network.Validations;
using GameShared.Packets;

namespace GameServer.Network.Middleware;

public sealed class PacketValidationMiddleware : IPacketMiddleware
{
    private readonly Dictionary<Type, IPacketValidator> _validators;
    private readonly ServerMetricsService _metrics;

    public PacketValidationMiddleware(IEnumerable<IPacketValidator> validators, ServerMetricsService metrics)
    {
        _metrics = metrics;
        _validators = validators
            .GroupBy(x => x.PacketType)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task InvokeAsync(ConnectionSession session, IPacket packet, Func<Task> next)
    {
        if (_validators.TryGetValue(packet.GetType(), out var validator) &&
            !validator.TryValidate(packet, out var errorPacket))
        {
            if (errorPacket is not null)
            {
                var profile = PacketTransportPolicy.Resolve(errorPacket);
                var data = PacketSerializer.Serialize(errorPacket);
                _metrics.RecordOutboundPacketSent(errorPacket.GetType().Name, data.Length);
                session.Peer.Send(data, profile.DeliveryMethod);
            }
            return;
        }

        await next();
    }
}
