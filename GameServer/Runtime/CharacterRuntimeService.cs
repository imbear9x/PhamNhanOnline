using System.Numerics;
using GameServer.DTO;
using GameServer.Network;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Logging;

namespace GameServer.Runtime;

public sealed class CharacterRuntimeService
{
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeCalculator _calculator;
    private readonly CharacterRuntimeNotifier _notifier;
    private readonly WorldInterestService _interestService;
    private readonly CharacterLifecycleService _lifecycleService;

    public CharacterRuntimeService(
        WorldManager worldManager,
        CharacterRuntimeCalculator calculator,
        CharacterRuntimeNotifier notifier,
        WorldInterestService interestService,
        CharacterLifecycleService lifecycleService)
    {
        _worldManager = worldManager;
        _calculator = calculator;
        _notifier = notifier;
        _interestService = interestService;
        _lifecycleService = lifecycleService;
    }

    public PlayerSession AttachPlayerSession(ConnectionSession session, CharacterSnapshotDto snapshot)
    {
        var baseStats = snapshot.BaseStats
            ?? throw new InvalidOperationException("Base stats must exist before attaching runtime state.");
        var currentState = snapshot.CurrentState
            ?? throw new InvalidOperationException("Current state must exist before attaching runtime state.");
        var clampedCurrentState = _calculator.ClampCurrentStateToBaseStats(baseStats, currentState);
        if (clampedCurrentState.CurrentState == CharacterRuntimeStateCodes.Casting)
            clampedCurrentState = clampedCurrentState with { CurrentState = CharacterRuntimeStateCodes.Idle };

        var player = _worldManager.AddOrUpdatePlayer(
            session.PlayerId,
            session.ConnectionId,
            snapshot.Character,
            baseStats,
            clampedCurrentState);

        session.Player = player;
        session.SelectedCharacterId = snapshot.Character.CharacterId;
        var isRestricted =
            _lifecycleService.IsLifespanExpired(player.CharacterData, baseStats, clampedCurrentState) ||
            clampedCurrentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired ||
            clampedCurrentState.CurrentState == CharacterRuntimeStateCodes.CombatDead ||
            clampedCurrentState.IsExpired;
        player.SetCharacterActionsRestricted(isRestricted);
        session.AreCharacterActionsRestricted = isRestricted;
        return player;
    }

    public CharacterRuntimeSnapshot ApplyDamage(PlayerSession player, int damage)
    {
        var utcNow = DateTime.UtcNow;
        var currentSnapshot = player.RuntimeState.CaptureSnapshot();
        var previousState = currentSnapshot.CurrentState;
        if (CharacterRuntimeStateCodes.IsDefeated(previousState))
        {
            return currentSnapshot;
        }

        var remainingDamage = player.CombatStatuses.AbsorbIncomingDamage(damage, utcNow, out _);
        if (remainingDamage <= 0)
            return player.RuntimeState.CaptureSnapshot();

        var baseStats = player.RuntimeState.CaptureSnapshot().BaseStats;
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.ApplyDamage(baseStats, current, remainingDamage));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        SyncCharacterActionRestriction(player, snapshot.CurrentState);
        _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        NotifyDeathTransitionIfNeeded(player, previousState, snapshot.CurrentState);
        return snapshot;
    }

    public CharacterRuntimeSnapshot RestoreResources(PlayerSession player, int hpDelta, int mpDelta)
    {
        return ApplyResourceDelta(player, hpDelta, mpDelta, 0);
    }

    public CharacterRuntimeSnapshot ApplyResourceDelta(PlayerSession player, int hpDelta, int mpDelta, int staminaDelta)
    {
        var previousState = player.RuntimeState.CaptureSnapshot().CurrentState;
        var baseStats = player.RuntimeState.CaptureSnapshot().BaseStats;
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.ApplyResourceDelta(baseStats, current, hpDelta, mpDelta, staminaDelta));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        SyncCharacterActionRestriction(player, snapshot.CurrentState);
        _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        NotifyDeathTransitionIfNeeded(player, previousState, snapshot.CurrentState);
        return snapshot;
    }

    public CharacterRuntimeSnapshot ApplyBaseStatsMutation(
        PlayerSession player,
        Func<CharacterBaseStatsDto, CharacterBaseStatsDto> mutation)
    {
        var baseStatsSnapshot = player.RuntimeState.UpdateBaseStats(mutation);
        var currentStateSnapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.ClampCurrentStateToBaseStats(baseStatsSnapshot.BaseStats, current));

        player.SynchronizeFromCurrentState(currentStateSnapshot.CurrentState);
        SyncCharacterActionRestriction(player, currentStateSnapshot.CurrentState);
        _notifier.NotifyBaseStatsChanged(player, baseStatsSnapshot.BaseStats);
        _notifier.NotifyCurrentStateChanged(player, currentStateSnapshot.CurrentState);
        _interestService.NotifyCurrentStateChanged(player, currentStateSnapshot.CurrentState);
        return currentStateSnapshot;
    }

    public CharacterRuntimeSnapshot ApplyCurrentStateMutation(
        PlayerSession player,
        Func<CharacterCurrentStateDto, CharacterCurrentStateDto> mutation,
        bool persist = true,
        bool notifySelf = true,
        bool notifyObservers = true)
    {
        var previousState = player.RuntimeState.CaptureSnapshot().CurrentState;
        var snapshot = player.RuntimeState.UpdateCurrentState(mutation, markDirty: persist);
        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        SyncCharacterActionRestriction(player, snapshot.CurrentState);

        if (notifySelf)
            _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);

        if (notifyObservers)
            _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);

        NotifyDeathTransitionIfNeeded(player, previousState, snapshot.CurrentState, notifySelf);

        return snapshot;
    }

    public CharacterRuntimeSnapshot UpdatePosition(PlayerSession player, int? mapId, Vector2 position)
    {
        return UpdatePosition(player, mapId, player.ZoneIndex, position, notifySelf: true);
    }

    public CharacterRuntimeSnapshot UpdatePosition(PlayerSession player, int? mapId, int? zoneIndex, Vector2 position)
    {
        return UpdatePosition(player, mapId, zoneIndex, position, notifySelf: true);
    }

    public CharacterRuntimeSnapshot UpdatePosition(PlayerSession player, int? mapId, int? zoneIndex, Vector2 position, bool notifySelf)
    {
        var previousSnapshot = player.RuntimeState.CaptureSnapshot();
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.UpdatePosition(current, mapId, zoneIndex, position));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        if (notifySelf)
            _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);

        _interestService.HandlePositionUpdated(player, previousSnapshot.CurrentState, snapshot.CurrentState);
        return snapshot;
    }

    public async Task RefreshTimeDerivedStateForOnlinePlayersAsync(CancellationToken cancellationToken = default)
    {
        foreach (var player in _worldManager.GetOnlinePlayersSnapshot())
        {
            var snapshot = player.RuntimeState.CaptureSnapshot();
            if (_lifecycleService.IsLifespanExpired(player.CharacterData, snapshot.BaseStats, snapshot.CurrentState))
                await _lifecycleService.HandleLifespanExpiredAsync(player, cancellationToken);
        }
    }

    private void SyncCharacterActionRestriction(PlayerSession player, CharacterCurrentStateDto currentState)
    {
        var restricted =
            _lifecycleService.IsLifespanExpired(player.CharacterData, player.RuntimeState.CaptureSnapshot().BaseStats, currentState) ||
            currentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired ||
            currentState.CurrentState == CharacterRuntimeStateCodes.CombatDead ||
            currentState.IsExpired;
        player.SetCharacterActionsRestricted(restricted);
    }

    private void NotifyDeathTransitionIfNeeded(
        PlayerSession player,
        CharacterCurrentStateDto previousState,
        CharacterCurrentStateDto currentState,
        bool notifySelf = true)
    {
        if (!notifySelf)
            return;

        var wasCombatDead = CharacterRuntimeStateCodes.IsCombatDead(previousState.CurrentState);
        var isCombatDead = CharacterRuntimeStateCodes.IsCombatDead(currentState.CurrentState);
        if (wasCombatDead || !isCombatDead || currentState.IsExpired)
            return;

        _notifier.NotifyStateTransition(player, CharacterStateTransitionReasons.CombatDead);
    }
}
