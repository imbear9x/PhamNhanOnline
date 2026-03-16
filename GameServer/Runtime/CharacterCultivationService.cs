using GameServer.DTO;
using GameServer.Entities;
using GameServer.Network;
using GameServer.Network.Interface;
using GameServer.Repositories;
using GameServer.Services;
using GameServer.World;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class CharacterCultivationService
{
    private const int PotentialPerCultivationPoint = 1;
    private const decimal GongPhapCoefficientStub = 1m;
    private const decimal FormationCoefficientStub = 1m;
    private static readonly TimeSpan SettlementIntervalValue = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterRuntimeNotifier _notifier;
    private readonly INetworkSender _network;
    private readonly MapCatalog _mapCatalog;

    public CharacterCultivationService(
        IServiceScopeFactory scopeFactory,
        WorldManager worldManager,
        CharacterRuntimeService runtimeService,
        CharacterRuntimeNotifier notifier,
        INetworkSender network,
        MapCatalog mapCatalog)
    {
        _scopeFactory = scopeFactory;
        _worldManager = worldManager;
        _runtimeService = runtimeService;
        _notifier = notifier;
        _network = network;
        _mapCatalog = mapCatalog;
    }

    public TimeSpan SettlementInterval => SettlementIntervalValue;

    public async Task<CultivationActionResult> StartCultivationAsync(ConnectionSession session, CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return CultivationActionResult.Failed(MessageCode.CharacterMustEnterWorld);

        var player = session.Player;
        var snapshot = player.RuntimeState.CaptureSnapshot();
        if (snapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Cultivating)
            return CultivationActionResult.Failed(MessageCode.CultivationAlreadyActive);

        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance) ||
            !instance.Definition.IsPrivatePerPlayer ||
            instance.Definition.Type != MapType.Home)
        {
            return CultivationActionResult.Failed(MessageCode.CultivationRequiresPrivateHome);
        }

        if (snapshot.CurrentState.IsDead || snapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired)
            return CultivationActionResult.Failed(MessageCode.CharacterActionsRestricted);

        var realms = await LoadRealmTemplatesAsync(cancellationToken);
        if (IsRealmCapReached(snapshot.BaseStats, realms))
            return CultivationActionResult.Failed(MessageCode.CultivationRealmCapReached);

        var utcNow = DateTime.UtcNow;
        var currentState = snapshot.CurrentState with
        {
            CurrentState = CharacterRuntimeStateCodes.Cultivating,
            CultivationStartedAtUtc = utcNow,
            LastCultivationRewardedAtUtc = utcNow,
            LastSavedAt = utcNow
        };

        _runtimeService.ApplyCurrentStateMutation(player, _ => currentState);
        return CultivationActionResult.Succeeded(snapshot.BaseStats, currentState);
    }

    public async Task<CultivationActionResult> StopCultivationAsync(ConnectionSession session, CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return CultivationActionResult.Failed(MessageCode.CharacterMustEnterWorld);

        var player = session.Player;
        var settled = await SettleOnlinePlayerAsync(player, isOfflineSettlement: false, notifyClient: true, DateTime.UtcNow, cancellationToken);
        var latestSnapshot = player.RuntimeState.CaptureSnapshot();
        if (latestSnapshot.CurrentState.CurrentState != CharacterRuntimeStateCodes.Cultivating)
            return CultivationActionResult.Failed(MessageCode.CultivationNotActive);

        var utcNow = DateTime.UtcNow;
        var stoppedState = latestSnapshot.CurrentState with
        {
            CurrentState = CharacterRuntimeStateCodes.Idle,
            CultivationStartedAtUtc = null,
            LastCultivationRewardedAtUtc = null,
            LastSavedAt = utcNow
        };

        _runtimeService.ApplyCurrentStateMutation(player, _ => stoppedState);
        return CultivationActionResult.Succeeded(
            latestSnapshot.BaseStats,
            stoppedState,
            settled.RewardEvent);
    }

    public async Task<CultivationActionResult> BreakthroughAsync(ConnectionSession session, CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return CultivationActionResult.Failed(MessageCode.CharacterMustEnterWorld);

        var player = session.Player;
        var settled = await SettleOnlinePlayerAsync(player, isOfflineSettlement: false, notifyClient: true, DateTime.UtcNow, cancellationToken);
        var snapshot = player.RuntimeState.CaptureSnapshot();
        var realms = await LoadRealmTemplatesAsync(cancellationToken);

        if (!snapshot.BaseStats.RealmTemplateId.HasValue ||
            !realms.TryGetValue(snapshot.BaseStats.RealmTemplateId.Value, out var currentRealm))
        {
            return CultivationActionResult.Failed(MessageCode.BreakthroughNotReady);
        }

        if ((snapshot.BaseStats.Cultivation ?? 0) < (currentRealm.MaxCultivation ?? long.MaxValue))
            return CultivationActionResult.Failed(MessageCode.BreakthroughNotReady);

        var nextRealmId = currentRealm.Id + 1;
        if (!realms.ContainsKey(nextRealmId))
            return CultivationActionResult.Failed(MessageCode.BreakthroughRealmMaxed);

        var updatedBaseStats = snapshot.BaseStats with
        {
            RealmTemplateId = nextRealmId
        };

        player.RuntimeState.UpdateBaseStats(_ => updatedBaseStats);
        _notifier.NotifyBaseStatsChanged(player, updatedBaseStats);
        var updatedSnapshot = player.RuntimeState.CaptureSnapshot();
        return CultivationActionResult.Succeeded(
            updatedSnapshot.BaseStats,
            updatedSnapshot.CurrentState,
            settled.RewardEvent);
    }

    public async Task<CultivationActionResult> AllocatePotentialAsync(
        ConnectionSession session,
        PotentialAllocationTarget target,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return CultivationActionResult.Failed(MessageCode.CharacterMustEnterWorld);
        if (amount <= 0)
            return CultivationActionResult.Failed(MessageCode.PotentialAllocationInvalid);
        if (target == PotentialAllocationTarget.None)
            return CultivationActionResult.Failed(MessageCode.PotentialTargetInvalid);

        var player = session.Player;
        var settled = await SettleOnlinePlayerAsync(player, isOfflineSettlement: false, notifyClient: true, DateTime.UtcNow, cancellationToken);
        var snapshot = player.RuntimeState.CaptureSnapshot();
        var currentPotential = snapshot.BaseStats.UnallocatedPotential ?? 0;
        if (currentPotential < amount)
            return CultivationActionResult.Failed(MessageCode.PotentialAllocationInvalid);

        var updatedBaseStats = ApplyPotentialAllocation(snapshot.BaseStats, target, amount);
        if (updatedBaseStats == snapshot.BaseStats)
            return CultivationActionResult.Failed(MessageCode.PotentialTargetInvalid);

        player.RuntimeState.UpdateBaseStats(_ => updatedBaseStats);
        _notifier.NotifyBaseStatsChanged(player, updatedBaseStats);
        var updatedSnapshot = player.RuntimeState.CaptureSnapshot();
        return CultivationActionResult.Succeeded(
            updatedSnapshot.BaseStats,
            updatedSnapshot.CurrentState,
            settled.RewardEvent);
    }

    public async Task<CultivationSnapshotSettlementResult> SettleSnapshotAsync(
        CharacterSnapshotDto snapshot,
        CancellationToken cancellationToken = default)
    {
        if (snapshot.BaseStats is null || snapshot.CurrentState is null)
            return CultivationSnapshotSettlementResult.Unchanged(snapshot);

        var realms = await LoadRealmTemplatesAsync(cancellationToken);
        if (!TryResolveCultivationMapDefinition(snapshot.CurrentState, out var mapDefinition) ||
            !snapshot.BaseStats.RealmTemplateId.HasValue ||
            !realms.TryGetValue(snapshot.BaseStats.RealmTemplateId.Value, out var realm))
        {
            return CultivationSnapshotSettlementResult.Unchanged(snapshot);
        }

        var grant = EvaluateGrant(snapshot.BaseStats, snapshot.CurrentState, realm, mapDefinition, DateTime.UtcNow);
        if (!grant.HasPersistenceChange)
            return CultivationSnapshotSettlementResult.Unchanged(snapshot);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();
        var updatedSnapshot = await characterService.UpdateCharacterCultivationAsync(
            grant.UpdatedBaseStats,
            grant.UpdatedCurrentState,
            cancellationToken);

        var rewardEvent = grant.HasReward
            ? CreateRewardEvent(grant, updatedSnapshot.Character.CharacterId, isOfflineSettlement: true)
            : null;
        return new CultivationSnapshotSettlementResult(updatedSnapshot, rewardEvent);
    }

    public async Task SettleCultivationAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var onlineCharacterIds = new HashSet<Guid>();
        foreach (var player in _worldManager.GetOnlinePlayersSnapshot())
        {
            onlineCharacterIds.Add(player.CharacterData.CharacterId);
            await SettleOnlinePlayerAsync(player, isOfflineSettlement: false, notifyClient: true, utcNow, cancellationToken);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();
        var realms = await LoadRealmTemplatesAsync(scope.ServiceProvider, cancellationToken);
        var cultivatingSnapshots = await characterService.ListCultivatingCharacterSnapshotsAsync(onlineCharacterIds, cancellationToken);

        foreach (var snapshot in cultivatingSnapshots)
        {
            if (snapshot.BaseStats is null || snapshot.CurrentState is null)
                continue;

            if (!TryResolveCultivationMapDefinition(snapshot.CurrentState, out var mapDefinition) ||
                !snapshot.BaseStats.RealmTemplateId.HasValue ||
                !realms.TryGetValue(snapshot.BaseStats.RealmTemplateId.Value, out var realm))
            {
                continue;
            }

            var grant = EvaluateGrant(snapshot.BaseStats, snapshot.CurrentState, realm, mapDefinition, utcNow);
            if (!grant.HasPersistenceChange)
                continue;

            await characterService.UpdateCharacterCultivationAsync(grant.UpdatedBaseStats, grant.UpdatedCurrentState, cancellationToken);
        }
    }

    public bool IsCultivating(CharacterCurrentStateDto? state)
    {
        return state is not null && state.CurrentState == CharacterRuntimeStateCodes.Cultivating;
    }

    public bool IsCultivating(PlayerSession? player)
    {
        if (player is null)
            return false;

        return player.RuntimeState.CaptureSnapshot().CurrentState.CurrentState == CharacterRuntimeStateCodes.Cultivating;
    }

    private async Task<OnlineSettlementResult> SettleOnlinePlayerAsync(
        PlayerSession player,
        bool isOfflineSettlement,
        bool notifyClient,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var snapshot = player.RuntimeState.CaptureSnapshot();
        var realms = await LoadRealmTemplatesAsync(cancellationToken);
        if (!TryResolveCultivationMapDefinition(snapshot.CurrentState, out var mapDefinition) ||
            !snapshot.BaseStats.RealmTemplateId.HasValue ||
            !realms.TryGetValue(snapshot.BaseStats.RealmTemplateId.Value, out var realm))
        {
            return OnlineSettlementResult.None;
        }

        var grant = EvaluateGrant(snapshot.BaseStats, snapshot.CurrentState, realm, mapDefinition, utcNow);
        if (!grant.HasPersistenceChange)
            return OnlineSettlementResult.None;

        player.RuntimeState.UpdateBaseStats(_ => grant.UpdatedBaseStats);
        player.RuntimeState.UpdateCurrentState(_ => grant.UpdatedCurrentState);
        player.SynchronizeFromCurrentState(grant.UpdatedCurrentState);

        if (grant.HasReward)
        {
            _notifier.NotifyBaseStatsChanged(player, grant.UpdatedBaseStats);
            if (notifyClient)
            {
                var rewardEvent = CreateRewardEvent(grant, player.CharacterData.CharacterId, isOfflineSettlement);
                _network.Send(player.ConnectionId, rewardEvent.ToPacket());
                return new OnlineSettlementResult(rewardEvent);
            }
        }

        return OnlineSettlementResult.None;
    }

    private static CharacterBaseStatsDto ApplyPotentialAllocation(
        CharacterBaseStatsDto baseStats,
        PotentialAllocationTarget target,
        int amount)
    {
        var remainingPotential = (baseStats.UnallocatedPotential ?? 0) - amount;
        if (remainingPotential < 0)
            return baseStats;

        return target switch
        {
            PotentialAllocationTarget.BaseHp => baseStats with
            {
                BaseHp = (baseStats.BaseHp ?? 0) + amount,
                UnallocatedPotential = remainingPotential
            },
            PotentialAllocationTarget.BaseMp => baseStats with
            {
                BaseMp = (baseStats.BaseMp ?? 0) + amount,
                UnallocatedPotential = remainingPotential
            },
            PotentialAllocationTarget.BasePhysique => baseStats with
            {
                BasePhysique = (baseStats.BasePhysique ?? 0) + amount,
                UnallocatedPotential = remainingPotential
            },
            PotentialAllocationTarget.BaseAttack => baseStats with
            {
                BaseAttack = (baseStats.BaseAttack ?? 0) + amount,
                UnallocatedPotential = remainingPotential
            },
            PotentialAllocationTarget.BaseSpeed => baseStats with
            {
                BaseSpeed = (baseStats.BaseSpeed ?? 0) + amount,
                UnallocatedPotential = remainingPotential
            },
            PotentialAllocationTarget.BaseSpiritualSense => baseStats with
            {
                BaseSpiritualSense = (baseStats.BaseSpiritualSense ?? 0) + amount,
                UnallocatedPotential = remainingPotential
            },
            PotentialAllocationTarget.BaseStamina => baseStats with
            {
                BaseStamina = (baseStats.BaseStamina ?? 0) + amount,
                UnallocatedPotential = remainingPotential
            },
            _ => baseStats
        };
    }

    private CultivationGrant EvaluateGrant(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState,
        RealmTemplate realm,
        MapDefinition mapDefinition,
        DateTime utcNow)
    {
        if (currentState.CurrentState != CharacterRuntimeStateCodes.Cultivating ||
            currentState.IsDead ||
            currentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired)
        {
            return CultivationGrant.None(baseStats, currentState);
        }

        var rewardedFrom = currentState.LastCultivationRewardedAtUtc
                           ?? currentState.CultivationStartedAtUtc
                           ?? utcNow;
        if (rewardedFrom > utcNow)
            rewardedFrom = utcNow;

        var maxCultivation = realm.MaxCultivation ?? long.MaxValue;
        var currentCultivation = baseStats.Cultivation ?? 0;
        var currentProgress = baseStats.CultivationProgress ?? 0m;
        if (currentCultivation >= maxCultivation)
        {
            if (currentState.LastCultivationRewardedAtUtc.HasValue &&
                currentState.LastCultivationRewardedAtUtc.Value >= utcNow &&
                currentProgress == 0m)
            {
                return CultivationGrant.None(baseStats, currentState);
            }

            return new CultivationGrant(
                baseStats with
                {
                    CultivationProgress = 0m
                },
                currentState with
                {
                    LastCultivationRewardedAtUtc = utcNow,
                    LastSavedAt = utcNow
                },
                0,
                0,
                rewardedFrom,
                utcNow,
                true,
                false,
                true);
        }

        var elapsed = utcNow - rewardedFrom;
        if (elapsed <= TimeSpan.Zero)
            return CultivationGrant.None(baseStats, currentState);

        var spiritualEnergyPerMinute = ResolveSpiritualEnergyPerMinute(currentState, mapDefinition);
        var rawCultivationGain = CalculateCultivationGain(elapsed, realm, spiritualEnergyPerMinute);
        if (rawCultivationGain <= 0m)
            return CultivationGrant.None(baseStats, currentState);

        var accumulatedProgress = currentProgress + rawCultivationGain;
        var remainingToCap = maxCultivation - currentCultivation;
        var grantedCultivation = Math.Min((long)decimal.Floor(accumulatedProgress), remainingToCap);
        var reachedRealmCap = currentCultivation + grantedCultivation >= maxCultivation;
        var grantedPotential = grantedCultivation <= 0
            ? 0
            : (int)Math.Min(int.MaxValue, grantedCultivation * PotentialPerCultivationPoint);
        var remainingProgress = accumulatedProgress - grantedCultivation;
        if (reachedRealmCap)
            remainingProgress = 0m;

        var updatedBaseStats = baseStats with
        {
            Cultivation = currentCultivation + grantedCultivation,
            UnallocatedPotential = checked((baseStats.UnallocatedPotential ?? 0) + grantedPotential),
            CultivationProgress = remainingProgress
        };
        var updatedCurrentState = currentState with
        {
            LastCultivationRewardedAtUtc = utcNow,
            LastSavedAt = utcNow
        };

        return new CultivationGrant(
            updatedBaseStats,
            updatedCurrentState,
            grantedCultivation,
            grantedPotential,
            rewardedFrom,
            utcNow,
            true,
            grantedCultivation > 0,
            reachedRealmCap);
    }

    private decimal CalculateCultivationGain(TimeSpan elapsed, RealmTemplate realm, decimal spiritualEnergyPerMinute)
    {
        var elapsedMinutes = elapsed.Ticks / (decimal)TimeSpan.TicksPerMinute;
        if (elapsedMinutes <= 0m)
            return 0m;

        return elapsedMinutes
               * spiritualEnergyPerMinute
               * (realm.AbsorptionMultiplier ?? 1m)
               * GongPhapCoefficientStub
               * FormationCoefficientStub;
    }

    private decimal ResolveSpiritualEnergyPerMinute(CharacterCurrentStateDto currentState, MapDefinition mapDefinition)
    {
        if (!currentState.CurrentMapId.HasValue)
            return mapDefinition.SpiritualEnergyPerMinute;

        if (currentState.CurrentZoneIndex > 0 &&
            _mapCatalog.TryGetZoneSlot(currentState.CurrentMapId.Value, currentState.CurrentZoneIndex, out var zoneSlot))
        {
            return zoneSlot.SpiritualEnergyPerMinute;
        }

        return mapDefinition.SpiritualEnergyPerMinute;
    }

    private bool TryResolveCultivationMapDefinition(CharacterCurrentStateDto currentState, out MapDefinition definition)
    {
        definition = null!;
        if (!currentState.CurrentMapId.HasValue)
            return false;

        if (!_mapCatalog.TryGet(currentState.CurrentMapId.Value, out definition))
            return false;

        return definition.Type == MapType.Home;
    }

    private async Task<Dictionary<int, RealmTemplate>> LoadRealmTemplatesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        return await LoadRealmTemplatesAsync(scope.ServiceProvider, cancellationToken);
    }

    private static async Task<Dictionary<int, RealmTemplate>> LoadRealmTemplatesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var repository = serviceProvider.GetRequiredService<RealmTemplateRepository>();
        var realms = await repository.GetAllAsync(cancellationToken);
        return realms.ToDictionary(x => x.Id);
    }

    private static bool IsRealmCapReached(CharacterBaseStatsDto baseStats, IReadOnlyDictionary<int, RealmTemplate> realms)
    {
        if (!baseStats.RealmTemplateId.HasValue || !realms.TryGetValue(baseStats.RealmTemplateId.Value, out var realm))
            return false;

        return (baseStats.Cultivation ?? 0) >= (realm.MaxCultivation ?? long.MaxValue);
    }

    private static CultivationRewardEvent CreateRewardEvent(CultivationGrant grant, Guid characterId, bool isOfflineSettlement)
    {
        return new CultivationRewardEvent(
            characterId,
            grant.CultivationGranted,
            grant.UnallocatedPotentialGranted,
            grant.ReachedRealmCap,
            isOfflineSettlement,
            grant.RewardedFromUtc,
            grant.RewardedToUtc);
    }

    public readonly record struct CultivationActionResult(
        bool Success,
        MessageCode Code,
        CharacterBaseStatsDto? BaseStats,
        CharacterCurrentStateDto? CurrentState,
        CultivationRewardEvent? RewardEvent)
    {
        public static CultivationActionResult Succeeded(
            CharacterBaseStatsDto? baseStats,
            CharacterCurrentStateDto? currentState,
            CultivationRewardEvent? rewardEvent = null)
        {
            return new CultivationActionResult(true, MessageCode.None, baseStats, currentState, rewardEvent);
        }

        public static CultivationActionResult Failed(MessageCode code)
        {
            return new CultivationActionResult(false, code, null, null, null);
        }
    }

    public readonly record struct CultivationSnapshotSettlementResult(
        CharacterSnapshotDto Snapshot,
        CultivationRewardEvent? RewardEvent)
    {
        public static CultivationSnapshotSettlementResult Unchanged(CharacterSnapshotDto snapshot)
        {
            return new CultivationSnapshotSettlementResult(snapshot, null);
        }
    }

    public sealed record CultivationRewardEvent(
        Guid CharacterId,
        long CultivationGranted,
        int UnallocatedPotentialGranted,
        bool ReachedRealmCap,
        bool IsOfflineSettlement,
        DateTime RewardedFromUtc,
        DateTime RewardedToUtc)
    {
        public CultivationRewardsGrantedPacket ToPacket()
        {
            return new CultivationRewardsGrantedPacket
            {
                CharacterId = CharacterId,
                CultivationGranted = CultivationGranted,
                UnallocatedPotentialGranted = UnallocatedPotentialGranted,
                ReachedRealmCap = ReachedRealmCap,
                IsOfflineSettlement = IsOfflineSettlement,
                RewardedFromUnixMs = ToUnixMs(RewardedFromUtc),
                RewardedToUnixMs = ToUnixMs(RewardedToUtc)
            };
        }

        private static long ToUnixMs(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
        }
    }

    private readonly record struct CultivationGrant(
        CharacterBaseStatsDto UpdatedBaseStats,
        CharacterCurrentStateDto UpdatedCurrentState,
        long CultivationGranted,
        int UnallocatedPotentialGranted,
        DateTime RewardedFromUtc,
        DateTime RewardedToUtc,
        bool HasPersistenceChange,
        bool HasReward,
        bool ReachedRealmCap)
    {
        public static CultivationGrant None(CharacterBaseStatsDto baseStats, CharacterCurrentStateDto currentState)
        {
            return new CultivationGrant(
                baseStats,
                currentState,
                0,
                0,
                currentState.LastCultivationRewardedAtUtc ?? currentState.CultivationStartedAtUtc ?? DateTime.UtcNow,
                currentState.LastCultivationRewardedAtUtc ?? currentState.CultivationStartedAtUtc ?? DateTime.UtcNow,
                false,
                false,
                false);
        }
    }

    private readonly record struct OnlineSettlementResult(CultivationRewardEvent? RewardEvent)
    {
        public static OnlineSettlementResult None => new(null);
    }
}
