using GameServer.Network.Interface;
using GameServer.Runtime;
using GameShared.Attributes;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Network.Middleware;

public sealed class CharacterActionRestrictionMiddleware : IPacketMiddleware
{
    private static readonly HashSet<Type> AuthPacketsRequiringCharacterAction = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => typeof(IPacket).IsAssignableFrom(t))
        .Where(t => t.IsDefined(typeof(RequireAuthAttribute), true))
        .ToHashSet();

    private static readonly HashSet<Type> AllowedWhenRestricted =
    [
        typeof(GetCharacterListPacket),
        typeof(GetCharacterDataPacket),
        typeof(EnterWorldPacket)
    ];

    private readonly IServiceProvider _serviceProvider;

    public CharacterActionRestrictionMiddleware(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(ConnectionSession session, IPacket packet, Func<Task> next)
    {
        var packetType = packet.GetType();
        var areCharacterActionsRestricted = session.AreCharacterActionsRestricted ||
                                            (session.Player?.AreCharacterActionsRestricted ?? false);
        if (!areCharacterActionsRestricted ||
            session.SelectedCharacterId == Guid.Empty ||
            !AuthPacketsRequiringCharacterAction.Contains(packetType) ||
            AllowedWhenRestricted.Contains(packetType))
        {
            await next();
            return;
        }

        Logger.Info($"Restricted character action rejected: {packetType.Name} (ConnectionId={session.ConnectionId}, CharacterId={session.SelectedCharacterId})");
        var networkSender = _serviceProvider.GetRequiredService<INetworkSender>();
        if (packet is CreateCharacterPacket)
        {
            networkSender.Send(session.ConnectionId, new CreateCharacterResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterActionsRestricted
            });
        }
        else if (packet is EnterWorldPacket)
        {
            networkSender.Send(session.ConnectionId, new EnterWorldResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterActionsRestricted
            });
        }

        networkSender.Send(session.ConnectionId, new CharacterStateTransitionPacket
        {
            CharacterId = session.SelectedCharacterId,
            Reason = CharacterStateTransitionReasons.LifespanExpired
        });
    }
}
