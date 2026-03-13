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
    private readonly GameTimeService _gameTimeService;

    public CharacterRuntimeService(
        WorldManager worldManager,
        CharacterRuntimeCalculator calculator,
        CharacterRuntimeNotifier notifier,
        GameTimeService gameTimeService)
    {
        _worldManager = worldManager;
        _calculator = calculator;
        _notifier = notifier;
        _gameTimeService = gameTimeService;
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
        return player;
    }

    public CharacterRuntimeSnapshot ApplyDamage(PlayerSession player, int damage)
    {
        var baseStats = player.RuntimeState.CaptureSnapshot().BaseStats;
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.ApplyDamage(baseStats, current, damage));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        return snapshot;
    }

    public CharacterRuntimeSnapshot RestoreResources(PlayerSession player, int hpDelta, int mpDelta)
    {
        var baseStats = player.RuntimeState.CaptureSnapshot().BaseStats;
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.RestoreResources(baseStats, current, hpDelta, mpDelta));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);
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
        return currentStateSnapshot;
    }

    public CharacterRuntimeSnapshot UpdatePosition(PlayerSession player, int? mapId, Vector2 position)
    {
        var snapshot = player.RuntimeState.UpdateCurrentState(
            current => _calculator.UpdatePosition(current, mapId, position));

        player.SynchronizeFromCurrentState(snapshot.CurrentState);
        _notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        return snapshot;
    }

    public void RefreshTimeDerivedStateForOnlinePlayers()
    {
        foreach (var player in _worldManager.GetOnlinePlayersSnapshot())
        {
            var snapshot = player.RuntimeState.CaptureSnapshot();
            _notifier.TryNotifyTimeDerivedCurrentStateChanged(player, snapshot.CurrentState);
        }
    }
}
