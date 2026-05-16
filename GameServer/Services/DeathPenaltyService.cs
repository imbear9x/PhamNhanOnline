using GameServer.Config;
using GameServer.DTO;
using GameServer.Runtime;
using GameServer.World;

namespace GameServer.Services;

public sealed class DeathPenaltyService
{
    private readonly CharacterService _characterService;
    private readonly GameConfigValues _gameConfig;

    public DeathPenaltyService(
        CharacterService characterService,
        GameConfigValues gameConfig)
    {
        _characterService = characterService;
        _gameConfig = gameConfig;
    }

    public async Task ApplyOnCombatDeathAsync(PlayerSession player, CancellationToken cancellationToken = default)
    {
        var runtimeSnapshot = player.RuntimeState.CaptureSnapshot();
        var baseStats = runtimeSnapshot.BaseStats;
        var currentState = runtimeSnapshot.CurrentState;
        var realmId = baseStats.RealmTemplateId ?? 0;
        var utcNow = DateTime.UtcNow;

        player.CombatStatuses.ClearBySource(CombatStatusSourceType.Skill);

        if (realmId >= 19)
        {
            var nextTribulationAtUtc = currentState.NextTribulationAtUtc ?? utcNow;
            var adjustedTribulationAtUtc = nextTribulationAtUtc.AddSeconds(-Math.Max(0, _gameConfig.DeathTribulationPenaltySeconds));
            if (adjustedTribulationAtUtc < utcNow)
                adjustedTribulationAtUtc = utcNow;

            var updatedState = currentState with
            {
                NextTribulationAtUtc = adjustedTribulationAtUtc,
                LastSavedAt = utcNow
            };

            await _characterService.UpdateCharacterCurrentStateAsync(updatedState, cancellationToken);
            player.RuntimeState.UpdateCurrentState(_ => updatedState, markDirty: true);
            player.SynchronizeFromCurrentState(updatedState);
            return;
        }

        var penaltyDays = (int)Math.Ceiling(Math.Max(0, _gameConfig.DeathLifespanPenaltySeconds) / 86400d);
        var updatedBaseStats = baseStats with
        {
            LifespanBonus = (baseStats.LifespanBonus ?? 0) - penaltyDays
        };

        await _characterService.UpdateCharacterBaseStatsAsync(updatedBaseStats, cancellationToken);
        player.RuntimeState.UpdateBaseStats(_ => updatedBaseStats);
        player.UpdateCharacter(player.CharacterData with { PendingPermanentDeletion = false });

        var firstEnterWorldAtUtc = player.CharacterData.FirstEnterWorldAtUtc;
        var lifespanEndUtc = CharacterLifespanRules.ResolveLifespanEndUtc(
            firstEnterWorldAtUtc,
            updatedBaseStats,
            0);

        if (lifespanEndUtc.HasValue && CharacterLifespanRules.IsExpired(lifespanEndUtc.Value, utcNow))
        {
            await _characterService.MarkPendingPermanentDeletionAsync(player.CharacterData.CharacterId, true, cancellationToken);
            player.UpdateCharacter(player.CharacterData with { PendingPermanentDeletion = true });
        }
    }
}
