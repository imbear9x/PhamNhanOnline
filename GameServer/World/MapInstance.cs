using System.Numerics;
using GameServer.Randomness;
using GameServer.Runtime;

namespace GameServer.World;

public sealed partial class MapInstance
{
    private readonly object _sync = new();
    private readonly Dictionary<(int X, int Y), HashSet<Guid>> _playersByCell = new();
    private readonly Dictionary<Guid, (int X, int Y)> _playerCells = new();
    private readonly Dictionary<Guid, PlayerSession> _playersById = new();
    private readonly Dictionary<int, SpawnGroupRuntimeState> _spawnStateByGroupId = new();
    private readonly Queue<EnemyDeathRuntimeEvent> _pendingDeaths = new();
    private readonly Queue<EnemySpawnRuntimeEvent> _pendingEnemySpawns = new();
    private readonly Queue<EnemyDespawnRuntimeEvent> _pendingEnemyDespawns = new();
    private readonly Queue<EnemyHpChangedRuntimeEvent> _pendingEnemyHpChanges = new();
    private readonly Queue<PlayerDamageRuntimeEvent> _pendingPlayerDamages = new();
    private readonly Queue<SkillCastReleaseRuntimeEvent> _pendingSkillCastReleases = new();
    private readonly Queue<SkillImpactDueRuntimeEvent> _pendingSkillImpactDues = new();
    private readonly Queue<SkillImpactResolvedRuntimeEvent> _pendingSkillImpactResolutions = new();
    private readonly Queue<GroundRewardSpawnRuntimeEvent> _pendingGroundRewardSpawns = new();
    private readonly Queue<GroundRewardDespawnRuntimeEvent> _pendingGroundRewardDespawns = new();
    private readonly List<PendingSkillExecution> _pendingSkillExecutions = new();
    private readonly IRandomNumberProvider _random;
    private int _nextMonsterId = 1;
    private int _nextGroundRewardId = 1;
    private int _nextSkillExecutionId = 1;
    private DateTime? _completedAtUtc;

    public int InstanceId { get; }
    public int ZoneIndex { get; }
    public Guid? OwnerCharacterId { get; }
    public bool IsPrivate => OwnerCharacterId.HasValue && RuntimeKind == MapRuntimeKind.PrivateHome;
    public MapDefinition Definition { get; }
    public int MapId => Definition.MapId;
    public DateTime CreatedAtUtc { get; }
    public DateTime? EmptySinceUtc { get; private set; }
    public MapRuntimeKind RuntimeKind { get; }
    public MapInstanceConfigDefinition? InstanceConfig { get; }
    public DateTime? ExpiresAtUtc { get; }
    public DateTime? CompletedAtUtc => _completedAtUtc;

    public List<PlayerSession> Players { get; } = new();
    public List<MonsterEntity> Monsters { get; } = new();
    public List<GroundRewardEntity> GroundRewards { get; } = new();

    public MapInstance(
        int instanceId,
        int zoneIndex,
        MapDefinition definition,
        Guid? ownerCharacterId,
        MapRuntimeKind runtimeKind,
        MapInstanceConfigDefinition? instanceConfig,
        IEnumerable<MapEnemySpawnGroupDefinition> spawnGroups,
        EnemyDefinitionCatalog enemyDefinitions,
        IRandomNumberProvider random,
        DateTime utcNow)
    {
        InstanceId = instanceId;
        ZoneIndex = zoneIndex;
        Definition = definition;
        OwnerCharacterId = ownerCharacterId;
        RuntimeKind = runtimeKind;
        InstanceConfig = instanceConfig;
        _random = random;
        CreatedAtUtc = utcNow;
        ExpiresAtUtc = instanceConfig?.DurationSeconds is > 0
            ? utcNow.AddSeconds(instanceConfig.DurationSeconds.Value)
            : null;

        foreach (var group in spawnGroups)
        {
            var state = new SpawnGroupRuntimeState(group, utcNow.AddSeconds(group.InitialSpawnDelaySeconds));
            foreach (var entry in group.Entries)
            {
                if (!enemyDefinitions.TryGetEnemy(entry.EnemyTemplateId, out var enemyDefinition))
                    throw new InvalidOperationException($"Spawn group {group.Id} references missing enemy template {entry.EnemyTemplateId}.");

                state.EnemyDefinitions[entry.EnemyTemplateId] = enemyDefinition;
            }

            _spawnStateByGroupId[group.Id] = state;
        }
    }

    public int PlayerCount
    {
        get
        {
            lock (_sync)
            {
                return Players.Count;
            }
        }
    }

    public IReadOnlyCollection<PlayerSession> GetPlayersSnapshot(Guid? excludePlayerId = null)
    {
        lock (_sync)
        {
            if (!excludePlayerId.HasValue)
                return Players.ToArray();

            return Players.Where(player => player.PlayerId != excludePlayerId.Value).ToArray();
        }
    }

    public IReadOnlyCollection<MonsterEntity> GetEnemiesSnapshot()
    {
        lock (_sync)
        {
            return Monsters.ToArray();
        }
    }

    public IReadOnlyCollection<GroundRewardEntity> GetGroundRewardsSnapshot()
    {
        lock (_sync)
        {
            return GroundRewards.ToArray();
        }
    }

    private sealed class SpawnGroupRuntimeState
    {
        public SpawnGroupRuntimeState(MapEnemySpawnGroupDefinition group, DateTime? nextSpawnAtUtc)
        {
            Group = group;
            NextSpawnAtUtc = nextSpawnAtUtc;
            EnemyDefinitions = new Dictionary<int, EnemyDefinition>();
        }

        public MapEnemySpawnGroupDefinition Group { get; }
        public HashSet<int> AliveEnemyIds { get; } = [];
        public Dictionary<int, EnemyDefinition> EnemyDefinitions { get; }
        public bool InitialFillDone { get; set; }
        public DateTime? NextSpawnAtUtc { get; set; }
    }
}
