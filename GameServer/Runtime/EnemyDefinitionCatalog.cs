using System.Collections.ObjectModel;
using System.Numerics;
using GameServer.Entities;
using GameServer.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class EnemyDefinitionCatalog
{
    private readonly IReadOnlyDictionary<int, EnemyDefinition> _enemyById;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<MapEnemySpawnGroupDefinition>> _spawnGroupsByMapId;
    private readonly IReadOnlyDictionary<int, MapInstanceConfigDefinition> _instanceConfigsByMapId;

    public EnemyDefinitionCatalog(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var enemyTemplates = scope.ServiceProvider.GetRequiredService<EnemyTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();
        var enemySkills = scope.ServiceProvider.GetRequiredService<EnemyTemplateSkillRepository>().GetAllAsync().GetAwaiter().GetResult();
        var rewardRules = scope.ServiceProvider.GetRequiredService<EnemyRewardRuleRepository>().GetAllAsync().GetAwaiter().GetResult();
        var spawnGroups = scope.ServiceProvider.GetRequiredService<MapEnemySpawnGroupRepository>().GetAllAsync().GetAwaiter().GetResult();
        var spawnEntries = scope.ServiceProvider.GetRequiredService<MapEnemySpawnEntryRepository>().GetAllAsync().GetAwaiter().GetResult();
        var instanceConfigs = scope.ServiceProvider.GetRequiredService<MapInstanceConfigRepository>().GetAllAsync().GetAwaiter().GetResult();
        var randomTables = scope.ServiceProvider.GetRequiredService<GameRandomTableRepository>().GetAllAsync().GetAwaiter().GetResult();

        var randomTableIdsById = randomTables.ToDictionary(x => x.Id, x => x.TableId);
        var skillsByEnemyId = enemySkills
            .GroupBy(x => x.EnemyTemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<EnemySkillLoadoutDefinition>)g
                    .OrderBy(x => x.OrderIndex)
                    .ThenBy(x => x.Id)
                    .Select(x => new EnemySkillLoadoutDefinition(x.Id, x.EnemyTemplateId, x.SkillId, x.OrderIndex))
                    .ToArray());

        var rewardRulesByEnemyId = rewardRules
            .GroupBy(x => x.EnemyTemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<EnemyRewardRuleDefinition>)g
                    .OrderBy(x => x.OrderIndex)
                    .ThenBy(x => x.Id)
                    .Select(x =>
                    {
                        if (!randomTableIdsById.TryGetValue(x.GameRandomTableId, out var randomTableId))
                            throw new InvalidOperationException($"enemy_reward_rules row {x.Id} references missing game_random_table {x.GameRandomTableId}.");

                        return new EnemyRewardRuleDefinition(
                            x.Id,
                            x.EnemyTemplateId,
                            (RewardDeliveryType)x.DeliveryType,
                            (RewardTargetRule)x.TargetRule,
                            randomTableId,
                            Math.Max(1, x.RollCount),
                            x.OwnershipDurationSeconds,
                            x.FreeForAllDurationSeconds,
                            Math.Max(0, x.MinimumDamagePartsPerMillion),
                            x.OrderIndex);
                    })
                    .ToArray());

        _enemyById = new ReadOnlyDictionary<int, EnemyDefinition>(
            enemyTemplates.ToDictionary(
                x => x.Id,
                x => new EnemyDefinition(
                    x.Id,
                    x.Code,
                    x.Name,
                    (EnemyKind)x.Kind,
                    x.MaxHp,
                    x.BaseAttack,
                    (float)x.BaseMoveSpeed,
                    (float)x.PatrolRadius,
                    (float)x.DetectionRadius,
                    (float)x.CombatRadius,
                    x.EnableOutOfCombatRestore,
                    Math.Max(0, x.OutOfCombatRestoreDelaySeconds),
                    Math.Max(0, x.MinimumSkillIntervalMs),
                    Math.Max(0L, x.CultivationRewardTotal),
                    Math.Max(0, x.PotentialRewardTotal),
                    x.Description,
                    skillsByEnemyId.GetValueOrDefault(x.Id, Array.Empty<EnemySkillLoadoutDefinition>()),
                    rewardRulesByEnemyId.GetValueOrDefault(x.Id, Array.Empty<EnemyRewardRuleDefinition>()))));

        var entriesByGroupId = spawnEntries
            .GroupBy(x => x.SpawnGroupId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<MapEnemySpawnEntryDefinition>)g
                    .OrderBy(x => x.OrderIndex)
                    .ThenBy(x => x.Id)
                    .Select(x => new MapEnemySpawnEntryDefinition(x.Id, x.SpawnGroupId, x.EnemyTemplateId, Math.Max(1, x.Weight), x.OrderIndex))
                    .ToArray());

        _spawnGroupsByMapId = new ReadOnlyDictionary<int, IReadOnlyList<MapEnemySpawnGroupDefinition>>(
            spawnGroups
                .GroupBy(x => x.MapTemplateId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<MapEnemySpawnGroupDefinition>)g
                        .OrderBy(x => x.Id)
                        .Select(x => new MapEnemySpawnGroupDefinition(
                            x.Id,
                            x.Code,
                            x.Name,
                            x.MapTemplateId,
                            (MapSpawnRuntimeScope)x.RuntimeScope,
                            x.ZoneIndex,
                            (EnemySpawnMode)x.SpawnMode,
                            x.IsBossSpawn,
                            Math.Max(1, x.MaxAlive),
                            Math.Max(0, x.RespawnSeconds),
                            Math.Max(0, x.InitialSpawnDelaySeconds),
                            new Vector2(x.CenterX, x.CenterY),
                            Math.Max(0f, x.SpawnRadius),
                            x.Description,
                            entriesByGroupId.GetValueOrDefault(x.Id, Array.Empty<MapEnemySpawnEntryDefinition>())))
                        .ToArray()));

        _instanceConfigsByMapId = new ReadOnlyDictionary<int, MapInstanceConfigDefinition>(
            instanceConfigs.ToDictionary(
                x => x.MapTemplateId,
                x => new MapInstanceConfigDefinition(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.MapTemplateId,
                    (InstanceMode)x.InstanceMode,
                    x.DurationSeconds,
                    x.IdleDestroySeconds,
                    (InstanceCompletionRule)x.CompletionRule,
                    x.CompleteDestroyDelaySeconds,
                    x.Description)));
    }

    public bool TryGetEnemy(int enemyTemplateId, out EnemyDefinition definition) =>
        _enemyById.TryGetValue(enemyTemplateId, out definition!);

    public IReadOnlyList<MapEnemySpawnGroupDefinition> GetSpawnGroupsForMap(int mapTemplateId) =>
        _spawnGroupsByMapId.GetValueOrDefault(mapTemplateId, Array.Empty<MapEnemySpawnGroupDefinition>());

    public bool TryGetInstanceConfig(int mapTemplateId, out MapInstanceConfigDefinition definition) =>
        _instanceConfigsByMapId.TryGetValue(mapTemplateId, out definition!);
}
