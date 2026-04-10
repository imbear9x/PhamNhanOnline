using GameServer.DTO;
using GameServer.Network;
using GameServer.Network.Interface;
using GameServer.Repositories;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Services;

public sealed class PlayerNotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorldManager _worldManager;
    private readonly INetworkSender _network;

    public PlayerNotificationService(
        IServiceScopeFactory scopeFactory,
        WorldManager worldManager,
        INetworkSender network)
    {
        _scopeFactory = scopeFactory;
        _worldManager = worldManager;
        _network = network;
    }

    public async Task PushUnreadAsync(ConnectionSession session, CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerNotificationRepository>();
        var builder = scope.ServiceProvider.GetRequiredService<PlayerNotificationModelBuilder>();
        var unread = await repository.ListUnreadByPlayerIdAsync(session.Player.CharacterData.CharacterId, cancellationToken);
        for (var i = 0; i < unread.Count; i++)
            Send(session.ConnectionId, builder.Build(unread[i]));
    }

    public async Task PushToOnlinePlayerAsync(Guid playerId, long notificationId, CancellationToken cancellationToken = default)
    {
        if (!_worldManager.TryGetPlayerByCharacterId(playerId, out var player))
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerNotificationRepository>();
        var builder = scope.ServiceProvider.GetRequiredService<PlayerNotificationModelBuilder>();
        var entity = await repository.GetByIdAsync(notificationId, cancellationToken);
        if (entity is null || entity.ReadAtUtc.HasValue || entity.PlayerId != playerId)
            return;

        Send(player.ConnectionId, builder.Build(entity));
    }

    public async Task<PlayerNotificationAcknowledgeResult> AcknowledgeAsync(
        ConnectionSession session,
        long? notificationId,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return PlayerNotificationAcknowledgeResult.Failed(MessageCode.CharacterMustEnterWorld);
        if (!notificationId.HasValue || notificationId.Value <= 0)
            return PlayerNotificationAcknowledgeResult.Failed(MessageCode.NotificationInvalid);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerNotificationRepository>();
        var entity = await repository.GetByIdAsync(notificationId.Value, cancellationToken);
        if (entity is null || entity.PlayerId != session.Player.CharacterData.CharacterId)
            return PlayerNotificationAcknowledgeResult.Failed(MessageCode.NotificationInvalid);

        if (!entity.ReadAtUtc.HasValue)
        {
            entity.ReadAtUtc = DateTime.UtcNow;
            await repository.UpdateAsync(entity, cancellationToken);
        }

        return PlayerNotificationAcknowledgeResult.Succeeded(entity.Id);
    }

    private void Send(int connectionId, GameShared.Models.PlayerNotificationModel notification)
    {
        _network.Send(connectionId, new PlayerNotificationReceivedPacket
        {
            Notification = notification
        });
    }
}

public readonly record struct PlayerNotificationAcknowledgeResult(
    bool Success,
    MessageCode? Code,
    long? NotificationId)
{
    public static PlayerNotificationAcknowledgeResult Succeeded(long notificationId) =>
        new(true, MessageCode.None, notificationId);

    public static PlayerNotificationAcknowledgeResult Failed(MessageCode code) =>
        new(false, code, null);
}
