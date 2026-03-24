using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.DTO;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class AttackEnemyHandler : IPacketHandler<AttackEnemyPacket>
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly INetworkSender _network;
    private readonly WorldManager _worldManager;

    public AttackEnemyHandler(
        CharacterCultivationService cultivationService,
        INetworkSender network,
        WorldManager worldManager)
    {
        _cultivationService = cultivationService;
        _network = network;
        _worldManager = worldManager;
    }

    public Task HandleAsync(ConnectionSession session, AttackEnemyPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                EnemyRuntimeId = packet.EnemyRuntimeId
            });
            return Task.CompletedTask;
        }

        var player = session.Player;
        if (_cultivationService.IsCultivating(player))
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterCannotMoveWhileCultivating,
                EnemyRuntimeId = packet.EnemyRuntimeId
            });
            return Task.CompletedTask;
        }

        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance))
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterNotInWorldInstance,
                EnemyRuntimeId = packet.EnemyRuntimeId
            });
            return Task.CompletedTask;
        }

        var snapshot = player.RuntimeState.CaptureSnapshot();
        var damage = Math.Max(1, snapshot.BaseStats.GetEffectiveAttack());
        var result = instance.ApplyEnemyDamage(player, packet.EnemyRuntimeId!.Value, damage, DateTime.UtcNow);

        _network.Send(session.ConnectionId, new AttackEnemyResultPacket
        {
            Success = result.Applied,
            Code = result.Applied ? MessageCode.None : result.Code,
            EnemyRuntimeId = packet.EnemyRuntimeId,
            DamageApplied = result.Applied ? damage : 0,
            RemainingHp = result.RemainingHp,
            IsKilled = result.IsKilled
        });

        return Task.CompletedTask;
    }
}
