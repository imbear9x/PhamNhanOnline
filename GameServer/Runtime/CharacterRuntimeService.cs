using System.Numerics;
using GameServer.DTO;
using GameServer.Network;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;

namespace GameServer.Runtime;

public sealed class CharacterRuntimeService
{
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeCalculator _calculator;
    private readonly CharacterRuntimeNotifier _notifier;
    private readonly WorldInterestService _interestService;
    private readonly GameTimeService _gameTimeService;
    private readonly CharacterLifecycleService _lifecycleService;

    public CharacterRuntimeService(
        WorldManager worldManager,
        CharacterRuntimeCalculator calculator,
        CharacterRuntimeNotifier notifier,
        WorldInterestService interestService,
        GameTimeService gameTimeService,
        CharacterLifecycleService lifecycleService)
    {
        _worldManager = worldManager;
        _calculator = calculator;
        _notifier = notifier;
        _interestService = interestService;
        _gameTimeService = gameTimeService;
        _lifecycleService = lifecycleService;
    }

    public PlayerSession AttachPlayerSession(ConnectionSession session, CharacterSnapshotDto snapshot)
    {
        var baseStats = snapshot.BaseStats
            ?? throw new InvalidOperationException("Base stats must exist before attaching runtime state.");
        var currentState = snapshot.CurrentState
            ?? throw new InvalidOperationException("Current state must exist before attaching runtime state.");

        var player = _worldManager.AddOrUpdatePlayer(
            session.PlayerId,
            session.ConnectionId,
            snapshot.Character,
            baseStats,
            currentState);

        session.Player = player;
        session.SelectedCharacterId = snapshot.Character.CharacterId;
        var currentRemaining = CharacterLifespanRules.CalculateRemainingLifespanYears(
            currentState.LifespanEndGameMinute,
            _gameTimeService.GetCurrentSnapshot());
        player.TryUpdateReportedRemainingLifespan(currentRemaining);
        var isRestricted = currentRemaining <= 0 || currentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired;
        player.SetCharacterActionsRestricted(isRestricted);
        session.AreCharacterActionsRestricted = isRestricted;
        return player;
    }

    public CharacterRuntimeSnapshot ApplyDamage(PlayerSession player, int damage)
    {
        var baseStats = player.RuntimeState.CaptureSnapshot().BaseStats;
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.ApplyDamage(baseStats, current, damage));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        return snapshot;
    }

    public CharacterRuntimeSnapshot RestoreResources(PlayerSession player, int hpDelta, int mpDelta)
    {
        var baseStats = player.RuntimeState.CaptureSnapshot().BaseStats;
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.RestoreResources(baseStats, current, hpDelta, mpDelta));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        return snapshot;
    }

    public CharacterRuntimeSnapshot ApplyBaseStatsMutation(
        PlayerSession player,
        Func<CharacterBaseStatsDto, CharacterBaseStatsDto> mutation)
    {
        var previousSnapshot = player.RuntimeState.CaptureSnapshot();
        var baseStatsSnapshot = player.RuntimeState.UpdateBaseStats(mutation);
        var currentStateSnapshot = player.RuntimeState.UpdateCurrentState(
            current =>
            {
                var clamped = _calculator.ClampCurrentStateToBaseStats(baseStatsSnapshot.BaseStats, current);
                var lifespanAdjusted = CharacterLifespanRules.AdjustLifespanEndGameMinute(
                    previousSnapshot.BaseStats,
                    baseStatsSnapshot.BaseStats,
                    clamped.LifespanEndGameMinute,
                    _gameTimeService.GetCurrentSnapshot());
                return clamped with { LifespanEndGameMinute = lifespanAdjusted };
            });

        player.SynchronizeFromCurrentState(currentStateSnapshot.CurrentState);
        _notifier.NotifyBaseStatsChanged(player, baseStatsSnapshot.BaseStats);
        _notifier.NotifyCurrentStateChanged(player, currentStateSnapshot.CurrentState);
        _interestService.NotifyCurrentStateChanged(player, currentStateSnapshot.CurrentState);
        return currentStateSnapshot;
    }

    public CharacterRuntimeSnapshot ApplyCurrentStateMutation(
        PlayerSession player,
        Func<CharacterCurrentStateDto, CharacterCurrentStateDto> mutation,
        bool notifySelf = true,
        bool notifyObservers = true)
    {
        var snapshot = player.RuntimeState.UpdateCurrentState(mutation);
        player.SynchronizeFromCurrentState(snapshot.CurrentState);

        if (notifySelf)
            _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);

        if (notifyObservers)
            _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);

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
            if (_lifecycleService.IsLifespanExpired(snapshot.CurrentState))
            {
                await _lifecycleService.HandleLifespanExpiredAsync(player, cancellationToken);
                continue;
            }

            if (_notifier.TryNotifyTimeDerivedCurrentStateChanged(player, snapshot.CurrentState))
            {
                _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);
            }
        }
    }
}
