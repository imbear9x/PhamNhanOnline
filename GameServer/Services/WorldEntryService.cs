using GameServer.DTO;
using GameServer.Network;
using GameServer.Runtime;
using GameServer.Time;
using GameServer.World;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Services;

public sealed class WorldEntryService
{
    private readonly CharacterService _characterService;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly CharacterLifecycleService _lifecycleService;
    private readonly CharacterCombatDeathRecoveryService _deathRecoveryService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly PracticeService _practiceService;
    private readonly AlchemyPracticeService _alchemyPracticeService;
    private readonly WorldInterestService _interestService;
    private readonly MapManager _mapManager;
    private readonly GameTimeService _gameTimeService;

    public WorldEntryService(
        CharacterService characterService,
        CharacterRuntimeService runtimeService,
        CharacterFinalStatService characterFinalStatService,
        CharacterLifecycleService lifecycleService,
        CharacterCombatDeathRecoveryService deathRecoveryService,
        CharacterCultivationService cultivationService,
        PracticeService practiceService,
        AlchemyPracticeService alchemyPracticeService,
        WorldInterestService interestService,
        MapManager mapManager,
        GameTimeService gameTimeService)
    {
        _characterService = characterService;
        _runtimeService = runtimeService;
        _characterFinalStatService = characterFinalStatService;
        _lifecycleService = lifecycleService;
        _deathRecoveryService = deathRecoveryService;
        _cultivationService = cultivationService;
        _practiceService = practiceService;
        _alchemyPracticeService = alchemyPracticeService;
        _interestService = interestService;
        _mapManager = mapManager;
        _gameTimeService = gameTimeService;
    }

    public async Task<WorldEntryActionResult> EnterAsync(
        ConnectionSession session,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var data = await _characterService.LoadCharacterSnapshotByAccountAsync(
            session.PlayerId,
            characterId,
            cancellationToken);

        if (data is null)
            return WorldEntryActionResult.Failure(MessageCode.CharacterNotFound);

        var cultivationSettlement = await _cultivationService.SettleSnapshotAsync(data);
        data = cultivationSettlement.Snapshot;
        await _alchemyPracticeService.EnsureDueSessionCompletedAsync(data.Character.CharacterId, cancellationToken);
        data = await _practiceService.AlignSnapshotStateAsync(data, cancellationToken);
        var ensuredCharacter = await _characterService.EnsureFirstEnterWorldAtUtcAsync(data.Character.CharacterId, cancellationToken);
        data = data with { Character = ensuredCharacter };
        data = await _lifecycleService.PrepareSnapshotForWorldEntryAsync(data);
        data = await _deathRecoveryService.RecoverSnapshotToHomeAsync(data);
        var isLifespanExpired = _lifecycleService.IsLifespanExpired(data);

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
        var currentStateModel = runtimeSnapshot.CurrentState.ToModel(
            player.CharacterData,
            runtimeSnapshot.BaseStats,
            _gameTimeService.GetCurrentSnapshot());

        return WorldEntryActionResult.SuccessResult(
            player,
            player.CharacterData.ToModel(),
            runtimeSnapshot.BaseStats.ToModel(),
            currentStateModel,
            cultivationSettlement.RewardEvent?.ToPacket(),
            isLifespanExpired ? MessageCode.CharacterLifespanExpired : MessageCode.None,
            isLifespanExpired);
    }
}

public readonly record struct WorldEntryActionResult(
    bool Success,
    MessageCode Code,
    PlayerSession? Player,
    CharacterModel? Character,
    CharacterBaseStatsModel? BaseStats,
    CharacterCurrentStateModel? CurrentState,
    CultivationRewardsGrantedPacket? RewardPacket,
    bool NotifyLifespanExpired)
{
    public static WorldEntryActionResult Failure(MessageCode code) =>
        new(false, code, null, null, null, null, null, false);

    public static WorldEntryActionResult SuccessResult(
        PlayerSession player,
        CharacterModel character,
        CharacterBaseStatsModel baseStats,
        CharacterCurrentStateModel currentState,
        CultivationRewardsGrantedPacket? rewardPacket,
        MessageCode code,
        bool notifyLifespanExpired) =>
        new(true, code, player, character, baseStats, currentState, rewardPacket, notifyLifespanExpired);
}
