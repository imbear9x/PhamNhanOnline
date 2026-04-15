using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;
using GameServer.Config;

namespace GameServer.Runtime;

public sealed class CharacterLifecycleService
{
    private const string LifespanExpiredNotificationTitle = "Tho nguyen da can";
    private const string LifespanExpiredNotificationMessage = "Nhan vat da het tho nguyen. Nhan xac nhan de quay lai man hinh dang nhap.";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;
    private readonly WorldInterestService _interestService;
    private readonly PlayerNotificationService _notificationService;
    private readonly CharacterCreateConfig _characterCreateConfig;

    public CharacterLifecycleService(
        IServiceScopeFactory scopeFactory,
        CharacterRuntimeSaveService runtimeSaveService,
        INetworkSender network,
        GameTimeService gameTimeService,
        WorldInterestService interestService,
        PlayerNotificationService notificationService,
        CharacterCreateConfig characterCreateConfig)
    {
        _scopeFactory = scopeFactory;
        _runtimeSaveService = runtimeSaveService;
        _network = network;
        _gameTimeService = gameTimeService;
        _interestService = interestService;
        _notificationService = notificationService;
        _characterCreateConfig = characterCreateConfig;
    }

    public bool IsLifespanExpired(CharacterSnapshotDto? snapshot)
    {
        if (snapshot is null || snapshot.BaseStats is null || snapshot.CurrentState is null)
            return false;

        var lifespanEndUtc = CharacterLifespanRules.ResolveLifespanEndUtc(
            snapshot.Character.FirstEnterWorldAtUtc,
            snapshot.BaseStats,
            _characterCreateConfig.FallbackRealmLifespanDays);
        return lifespanEndUtc.HasValue && CharacterLifespanRules.IsExpired(lifespanEndUtc.Value, DateTime.UtcNow);
    }

    public async Task<CharacterSnapshotDto> PrepareSnapshotForWorldEntryAsync(
        CharacterSnapshotDto snapshot,
        CancellationToken cancellationToken = default)
    {
        if (!IsLifespanExpired(snapshot) || snapshot.CurrentState is null)
            return snapshot;

        var updatedState = await PersistLifespanExpiredStateIfNeededAsync(snapshot.CurrentState, cancellationToken);
        await EnsureLifespanExpiredNotificationAsync(snapshot.Character.CharacterId, pushIfOnline: false, cancellationToken);
        return snapshot with { CurrentState = updatedState };
    }

    public bool IsLifespanExpired(CharacterDto character, CharacterBaseStatsDto? baseStats, CharacterCurrentStateDto? currentState)
    {
        if (baseStats is null || currentState is null)
            return false;

        var snapshot = new CharacterSnapshotDto(character, baseStats, currentState);
        return IsLifespanExpired(snapshot);
    }

    public async Task HandleLifespanExpiredAsync(PlayerSession player, CancellationToken cancellationToken = default)
    {
        var snapshot = player.RuntimeState.UpdateCurrentState(MarkLifespanExpired);
        player.SetCharacterActionsRestricted(true);
        player.SynchronizeFromCurrentState(snapshot.CurrentState);

        if (player.IsLifespanExpiredProcessed)
            return;

        player.MarkLifespanExpiredProcessed();
        await EnsureLifespanExpiredNotificationAsync(player.CharacterData.CharacterId, pushIfOnline: true, cancellationToken);
        _network.Send(player.ConnectionId, new CharacterCurrentStateChangedPacket
        {
            CurrentState = snapshot.CurrentState.ToModel(
                player.CharacterData,
                player.RuntimeState.CaptureSnapshot().BaseStats,
                _gameTimeService.GetCurrentSnapshot(),
                _characterCreateConfig.FallbackRealmLifespanDays)
        });
        _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        _network.Send(player.ConnectionId, new CharacterStateTransitionPacket
        {
            CharacterId = player.CharacterData.CharacterId,
            Reason = CharacterStateTransitionReasons.LifespanExpired
        });

        await _runtimeSaveService.FlushPlayerAsync(player.PlayerId, cancellationToken);
    }

    public void NotifyLifespanExpired(int connectionId, Guid characterId)
    {
        _network.Send(connectionId, new CharacterStateTransitionPacket
        {
            CharacterId = characterId,
            Reason = CharacterStateTransitionReasons.LifespanExpired
        });
    }

    private async Task<CharacterCurrentStateDto> PersistLifespanExpiredStateIfNeededAsync(
        CharacterCurrentStateDto currentState,
        CancellationToken cancellationToken)
    {
        var expiredState = MarkLifespanExpired(currentState);
        if (expiredState == currentState)
            return currentState;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();
        return await characterService.UpdateCharacterCurrentStateAsync(expiredState, cancellationToken);
    }

    private static CharacterCurrentStateDto MarkLifespanExpired(CharacterCurrentStateDto currentState)
    {
        if (currentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired &&
            currentState.IsExpired &&
            currentState.CurrentHp == 0)
        {
            return currentState;
        }

        return currentState with
        {
            CurrentHp = 0,
            IsExpired = true,
            CurrentState = CharacterRuntimeStateCodes.LifespanExpired,
            LastSavedAt = DateTime.UtcNow
        };
    }

    private Task<long> EnsureLifespanExpiredNotificationAsync(
        Guid characterId,
        bool pushIfOnline,
        CancellationToken cancellationToken)
    {
        return _notificationService.EnsureUnreadAsync(
            new Entities.PlayerNotificationEntity
            {
                PlayerId = characterId,
                NotificationType = (int)PlayerNotificationType.LifespanExpired,
                SourceType = (int)PlayerNotificationSourceType.CharacterLifecycle,
                Title = LifespanExpiredNotificationTitle,
                Message = LifespanExpiredNotificationMessage,
                CreatedAtUtc = DateTime.UtcNow
            },
            pushIfOnline,
            cancellationToken);
    }
}
