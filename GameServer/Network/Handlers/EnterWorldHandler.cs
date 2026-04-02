using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class EnterWorldHandler : IPacketHandler<EnterWorldPacket>
{
    private readonly CharacterService _characterService;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly CharacterLifecycleService _lifecycleService;
    private readonly CharacterCombatDeathRecoveryService _deathRecoveryService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly WorldInterestService _interestService;
    private readonly MapManager _mapManager;
    private readonly INetworkSender _server;
    private readonly GameTimeService _gameTimeService;

    public EnterWorldHandler(
        CharacterService characterService,
        CharacterRuntimeService runtimeService,
        CharacterFinalStatService characterFinalStatService,
        CharacterLifecycleService lifecycleService,
        CharacterCombatDeathRecoveryService deathRecoveryService,
        CharacterCultivationService cultivationService,
        WorldInterestService interestService,
        MapManager mapManager,
        INetworkSender server,
        GameTimeService gameTimeService)
    {
        _characterService = characterService;
        _runtimeService = runtimeService;
        _characterFinalStatService = characterFinalStatService;
        _lifecycleService = lifecycleService;
        _deathRecoveryService = deathRecoveryService;
        _cultivationService = cultivationService;
        _interestService = interestService;
        _mapManager = mapManager;
        _server = server;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, EnterWorldPacket packet)
    {
        try
        {
            var data = await _characterService.LoadCharacterSnapshotByAccountAsync(
                session.PlayerId,
                packet.CharacterId!.Value);

            if (data is null)
            {
                _server.Send(session.ConnectionId, new EnterWorldResultPacket
                {
                    Success = false,
                    Code = MessageCode.CharacterNotFound
                });
                return;
            }

            var cultivationSettlement = await _cultivationService.SettleSnapshotAsync(data);
            data = cultivationSettlement.Snapshot;
            data = await _lifecycleService.PrepareSnapshotForWorldEntryAsync(data);
            data = await _deathRecoveryService.RecoverSnapshotToHomeAsync(data);
            var isLifespanExpired = _lifecycleService.IsLifespanExpired(data.CurrentState);

            session.SelectedCharacterId = data.Character.CharacterId;
            var player = _runtimeService.AttachPlayerSession(session, data);
            var preserveHeldWorldState = player.InstanceId != 0 &&
                                         _mapManager.TryGetInstance(player.MapId, player.InstanceId, out _);
            _interestService.EnsurePlayerInWorld(
                player,
                requestedZoneIndex: preserveHeldWorldState ? player.ZoneIndex : null,
                autoSelectPublicZone: !preserveHeldWorldState);
            if (isLifespanExpired)
            {
                player.SetCharacterActionsRestricted(true);
                session.AreCharacterActionsRestricted = true;
            }

            var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(player);

            _server.Send(session.ConnectionId, new EnterWorldResultPacket
            {
                Success = true,
                Code = isLifespanExpired ? MessageCode.CharacterLifespanExpired : MessageCode.None,
                Character = player.CharacterData.ToModel(),
                BaseStats = runtimeSnapshot.BaseStats.ToModel(),
                CurrentState = runtimeSnapshot.CurrentState.ToModel(_gameTimeService.GetCurrentSnapshot())
            });

            _interestService.PublishWorldSnapshot(player);

            if (cultivationSettlement.RewardEvent is not null)
                _server.Send(session.ConnectionId, cultivationSettlement.RewardEvent.ToPacket());

            if (isLifespanExpired)
                _lifecycleService.NotifyLifespanExpired(session.ConnectionId, data.Character.CharacterId);
        }
        catch (Exception)
        {
            _server.Send(session.ConnectionId, new EnterWorldResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError
            });
            throw;
        }
    }
}
