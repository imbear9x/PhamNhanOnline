using GameServer.DTO;
using GameServer.Services;
using GameServer.World;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class CharacterCombatDeathRecoveryService
{
    private const double ReturnHomeRecoveryRatio = 0.80d;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MapCatalog _mapCatalog;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;

    public CharacterCombatDeathRecoveryService(
        IServiceScopeFactory scopeFactory,
        MapCatalog mapCatalog,
        CharacterRuntimeSaveService runtimeSaveService)
    {
        _scopeFactory = scopeFactory;
        _mapCatalog = mapCatalog;
        _runtimeSaveService = runtimeSaveService;
    }

    public bool IsCombatDead(CharacterCurrentStateDto? currentState)
    {
        if (currentState is null)
            return false;

        if (CharacterRuntimeStateCodes.IsPermanentlyDead(currentState.CurrentState))
            return false;

        return currentState.IsDead || CharacterRuntimeStateCodes.IsCombatDead(currentState.CurrentState);
    }

    public async Task<CharacterSnapshotDto> RecoverSnapshotToHomeAsync(
        CharacterSnapshotDto snapshot,
        CancellationToken cancellationToken = default)
    {
        if (snapshot.BaseStats is null || snapshot.CurrentState is null || !IsCombatDead(snapshot.CurrentState))
            return snapshot;

        var recoveredState = BuildRecoveredState(snapshot.BaseStats, snapshot.CurrentState);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();
        var persistedState = await characterService.UpdateCharacterCurrentStateAsync(recoveredState, cancellationToken);
        return snapshot with { CurrentState = persistedState };
    }

    public async Task<CharacterRuntimeSnapshot?> RecoverOnlinePlayerToHomeAsync(
        PlayerSession player,
        CancellationToken cancellationToken = default)
    {
        var snapshot = player.RuntimeState.CaptureSnapshot();
        if (!IsCombatDead(snapshot.CurrentState))
            return null;

        var recoveredState = BuildRecoveredState(snapshot.BaseStats, snapshot.CurrentState);
        var updatedSnapshot = player.RuntimeState.UpdateCurrentState(_ => recoveredState, markDirty: true);

        player.SynchronizeFromCurrentState(updatedSnapshot.CurrentState);
        player.SetCharacterActionsRestricted(false);
        player.SetMapEntryContext(new MapEntryContext(
            MapEntryReason.DefaultSpawn,
            PortalId: null,
            SpawnPointId: null,
            new System.Numerics.Vector2(updatedSnapshot.CurrentState.CurrentPosX, updatedSnapshot.CurrentState.CurrentPosY)));

        using var scope = _scopeFactory.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<CharacterRuntimeNotifier>();
        var interestService = scope.ServiceProvider.GetRequiredService<WorldInterestService>();
        notifier.NotifyCurrentStateChanged(player, updatedSnapshot.CurrentState);
        interestService.EnsurePlayerInWorld(player, requestedZoneIndex: updatedSnapshot.CurrentState.CurrentZoneIndex, autoSelectPublicZone: false);
        interestService.PublishWorldSnapshot(player);
        await _runtimeSaveService.FlushPlayerAsync(player.PlayerId, cancellationToken);
        return updatedSnapshot;
    }

    public async Task<CharacterCurrentStateDto?> RecoverDisconnectedPlayerToHomeAsync(
        PlayerSession player,
        CancellationToken cancellationToken = default)
    {
        var snapshot = player.RuntimeState.CaptureSnapshot();
        if (!IsCombatDead(snapshot.CurrentState))
            return null;

        var recoveredState = BuildRecoveredState(snapshot.BaseStats, snapshot.CurrentState);
        var updatedSnapshot = player.RuntimeState.UpdateCurrentState(_ => recoveredState, markDirty: true);
        player.SynchronizeFromCurrentState(updatedSnapshot.CurrentState);
        player.SetCharacterActionsRestricted(false);
        await _runtimeSaveService.FlushPlayerAsync(player.PlayerId, cancellationToken);
        return updatedSnapshot.CurrentState;
    }

    private CharacterCurrentStateDto BuildRecoveredState(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState)
    {
        var homeDefinition = _mapCatalog.ResolveHomeDefinition();
        var recoveredHp = Math.Max(1, (int)Math.Ceiling(baseStats.GetEffectiveHp() * ReturnHomeRecoveryRatio));
        var recoveredMp = Math.Max(0, (int)Math.Ceiling(baseStats.GetEffectiveMp() * ReturnHomeRecoveryRatio));
        var maxStamina = Math.Max(0, baseStats.GetEffectiveStamina());
        var recoveredStamina = Math.Clamp(currentState.CurrentStamina, 0, maxStamina);

        return currentState with
        {
            CurrentHp = recoveredHp,
            CurrentMp = recoveredMp,
            CurrentStamina = recoveredStamina,
            CurrentMapId = homeDefinition.MapId,
            CurrentZoneIndex = homeDefinition.DefaultZoneIndex,
            CurrentPosX = homeDefinition.DefaultSpawnPosition.X,
            CurrentPosY = homeDefinition.DefaultSpawnPosition.Y,
            IsDead = false,
            CurrentState = CharacterRuntimeStateCodes.Idle,
            CultivationStartedAtUtc = null,
            LastSavedAt = DateTime.UtcNow
        };
    }
}
