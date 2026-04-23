using GameServer.Config;
using GameServer.Descriptions;
using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameShared.Enums;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class SkillService
{
    private const string MissingEquipmentGrantBlockedReason = "Skill tu trang bi hien khong kha dung.";
    private const string RealmRequirementBlockedReason = "Canh gioi hien tai chua du de gan skill nay vao loadout.";

    private readonly GameDb _db;
    private readonly CombatDefinitionCatalog _combatDefinitions;
    private readonly GameConfigValues _gameConfig;
    private readonly CharacterBaseStatRepository _characterBaseStats;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerEquipmentRepository _playerEquipments;
    private readonly PlayerSkillRepository _playerSkills;
    private readonly PlayerSkillGrantSourceRepository _playerSkillGrantSources;
    private readonly PlayerSkillLoadoutRepository _playerSkillLoadouts;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly GameplayDescriptionService _descriptions;

    public SkillService(
        GameDb db,
        CombatDefinitionCatalog combatDefinitions,
        GameConfigValues gameConfig,
        CharacterBaseStatRepository characterBaseStats,
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        PlayerSkillRepository playerSkills,
        PlayerSkillGrantSourceRepository playerSkillGrantSources,
        PlayerSkillLoadoutRepository playerSkillLoadouts,
        ItemDefinitionCatalog itemDefinitions,
        GameplayDescriptionService descriptions)
    {
        _db = db;
        _combatDefinitions = combatDefinitions;
        _gameConfig = gameConfig;
        _characterBaseStats = characterBaseStats;
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _playerSkills = playerSkills;
        _playerSkillGrantSources = playerSkillGrantSources;
        _playerSkillLoadouts = playerSkillLoadouts;
        _itemDefinitions = itemDefinitions;
        _descriptions = descriptions;
    }

    public async Task<OwnedSkillsSnapshotDto> GetOwnedSkillsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        var baseStats = await _characterBaseStats.GetByIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
        return await BuildSnapshotAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
    }

    public async Task<OwnedSkillsSnapshotDto> SwapSkillLoadoutSlotsAsync(
        Guid playerId,
        int sourceSlotIndex,
        int targetSlotIndex,
        CancellationToken cancellationToken = default)
    {
        if (sourceSlotIndex < 1 || sourceSlotIndex > _gameConfig.SkillMaxLoadoutSlotCount ||
            targetSlotIndex < 1 || targetSlotIndex > _gameConfig.SkillMaxLoadoutSlotCount)
        {
            throw new GameException(MessageCode.SkillLoadoutSlotInvalid);
        }

        if (sourceSlotIndex == targetSlotIndex)
            return await GetOwnedSkillsAsync(playerId, cancellationToken);

        var baseStats = await _characterBaseStats.GetByIdAsync(playerId, cancellationToken);
        var currentRealmTemplateId = baseStats?.RealmId;
        var playerItems = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var playerItemsById = playerItems.ToDictionary(x => x.Id);
        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);

        var sourceLoadout = loadouts.FirstOrDefault(x => x.SlotIndex == sourceSlotIndex);
        var targetLoadout = loadouts.FirstOrDefault(x => x.SlotIndex == targetSlotIndex);
        if (sourceLoadout is null || targetLoadout is null)
            throw new GameException(MessageCode.SkillLoadoutSlotEmpty);

        var sourceSkill = playerSkills.FirstOrDefault(x => x.Id == sourceLoadout.PlayerSkillId);
        var targetSkill = playerSkills.FirstOrDefault(x => x.Id == targetLoadout.PlayerSkillId);
        if (sourceSkill is null || targetSkill is null)
            throw new GameException(MessageCode.PlayerSkillInvalid);

        ValidateLoadoutChange(targetSlotIndex, sourceSkill, currentRealmTemplateId, playerItemsById);
        ValidateLoadoutChange(sourceSlotIndex, targetSkill, currentRealmTemplateId, playerItemsById);

        var tempSlotIndex = -Math.Max(sourceSlotIndex, targetSlotIndex);

        if (_db.Transaction is not null)
        {
            await SwapLoadoutRowsAsync(sourceLoadout, targetLoadout, sourceSlotIndex, targetSlotIndex, tempSlotIndex, cancellationToken);
        }
        else
        {
            await using var tx = await _db.BeginTransactionAsync(cancellationToken);
            await SwapLoadoutRowsAsync(sourceLoadout, targetLoadout, sourceSlotIndex, targetSlotIndex, tempSlotIndex, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
        return await BuildSnapshotAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
    }

    public async Task<OwnedSkillsSnapshotDto> SetSkillLoadoutSlotAsync(
        Guid playerId,
        int slotIndex,
        long? playerSkillId,
        CancellationToken cancellationToken = default)
    {
        if (slotIndex < 1 || slotIndex > _gameConfig.SkillMaxLoadoutSlotCount)
            throw new GameException(MessageCode.SkillLoadoutSlotInvalid);

        var baseStats = await _characterBaseStats.GetByIdAsync(playerId, cancellationToken);
        var currentRealmTemplateId = baseStats?.RealmId;
        var playerItems = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var playerItemsById = playerItems.ToDictionary(x => x.Id);
        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);

        var normalizedPlayerSkillId = playerSkillId.GetValueOrDefault();
        if (normalizedPlayerSkillId <= 0)
        {
            await ClearSlotAsync(playerId, slotIndex, loadouts, cancellationToken);
            loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
            (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
            return await BuildSnapshotAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
        }

        var playerSkill = playerSkills.FirstOrDefault(x => x.Id == normalizedPlayerSkillId);
        if (playerSkill is null)
            throw new GameException(MessageCode.PlayerSkillInvalid);

        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out _))
            throw new GameException(MessageCode.SkillNotLearned);

        ValidateLoadoutChange(slotIndex, playerSkill, currentRealmTemplateId, playerItemsById);

        await RemoveDuplicateLoadoutsAsync(playerId, normalizedPlayerSkillId, loadouts, cancellationToken);

        var targetLoadout = loadouts.FirstOrDefault(x => x.SlotIndex == slotIndex);
        if (targetLoadout is null)
        {
            targetLoadout = new PlayerSkillLoadoutEntity
            {
                PlayerId = playerId,
                SlotIndex = slotIndex,
                PlayerSkillId = normalizedPlayerSkillId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            targetLoadout.Id = await _playerSkillLoadouts.CreateAsync(targetLoadout, cancellationToken);
        }
        else
        {
            targetLoadout.PlayerSkillId = normalizedPlayerSkillId;
            targetLoadout.UpdatedAt = DateTime.UtcNow;
            await _playerSkillLoadouts.UpdateAsync(targetLoadout, cancellationToken);
        }

        loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
        return await BuildSnapshotAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
    }

    public async Task<SkillEquipmentGrantSyncResult> SyncEquipmentGrantedSkillsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillGrantSourceEntity> grantSources = await _playerSkillGrantSources.ListByPlayerIdAsync(playerId, cancellationToken);
        var playerItems = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var baseStats = await _characterBaseStats.GetByIdAsync(playerId, cancellationToken);

        var playerItemsById = playerItems.ToDictionary(x => x.Id);
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(playerItemsById.Keys.ToArray(), cancellationToken);
        var equippedSlotByPlayerItemId = equipmentRows
            .Where(x => x.EquippedSlot.HasValue && x.EquippedSlot.Value > 0)
            .ToDictionary(x => x.PlayerItemId, x => x.EquippedSlot!.Value);
        var equipmentCandidates = BuildEquipmentGrantCandidates(playerItemsById, equippedSlotByPlayerItemId).ToArray();
        var activeEquipmentSourceIds = equipmentCandidates
            .Select(x => x.PlayerItemId)
            .ToHashSet();

        var changed = false;
        var baselineResult = await EnsureNonEquipmentSourceRecordsAsync(playerSkills, grantSources, cancellationToken);
        grantSources = baselineResult.Sources;
        changed |= baselineResult.Changed;

        var playerSkillByGroupCode = playerSkills.ToDictionary(x => x.SkillGroupCode, StringComparer.Ordinal);
        foreach (var candidate in equipmentCandidates)
        {
            if (!playerSkillByGroupCode.TryGetValue(candidate.SkillGroupCode, out var playerSkill))
            {
                playerSkill = new PlayerSkillEntity
                {
                    PlayerId = playerId,
                    SkillId = candidate.SkillId,
                    SkillGroupCode = candidate.SkillGroupCode,
                    SourceType = (int)PlayerSkillSourceType.EquipmentGrant,
                    SourcePlayerItemId = candidate.PlayerItemId,
                    SourceMartialArtId = null,
                    SourceMartialArtSkillId = null,
                    UnlockedAt = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                playerSkill.Id = await _playerSkills.CreateAsync(playerSkill, cancellationToken);
                playerSkills = playerSkills.Append(playerSkill).ToArray();
                playerSkillByGroupCode[candidate.SkillGroupCode] = playerSkill;
                changed = true;
            }

            var existingSource = grantSources.FirstOrDefault(x =>
                x.PlayerSkillId == playerSkill.Id &&
                x.SourceType == (int)PlayerSkillSourceType.EquipmentGrant &&
                x.SourcePlayerItemId.HasValue &&
                x.SourcePlayerItemId.Value == candidate.PlayerItemId);

            if (existingSource is null)
            {
                var newSource = new PlayerSkillGrantSourceEntity
                {
                    PlayerId = playerId,
                    PlayerSkillId = playerSkill.Id,
                    SourceType = (int)PlayerSkillSourceType.EquipmentGrant,
                    GrantedSkillId = candidate.SkillId,
                    SourcePlayerItemId = candidate.PlayerItemId,
                    SourceEquipmentTemplateId = candidate.EquipmentTemplateId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                newSource.Id = await _playerSkillGrantSources.CreateAsync(newSource, cancellationToken);
                grantSources = grantSources.Append(newSource).ToArray();
                changed = true;
                continue;
            }

            var sourceRequiresUpdate = existingSource.GrantedSkillId != candidate.SkillId ||
                                       existingSource.SourceEquipmentTemplateId != candidate.EquipmentTemplateId;
            if (!sourceRequiresUpdate)
                continue;

            existingSource.GrantedSkillId = candidate.SkillId;
            existingSource.SourceEquipmentTemplateId = candidate.EquipmentTemplateId;
            existingSource.UpdatedAt = DateTime.UtcNow;
            await _playerSkillGrantSources.UpdateAsync(existingSource, cancellationToken);
            changed = true;
        }

        for (var i = 0; i < grantSources.Count; i++)
        {
            var source = grantSources[i];
            if (source.SourceType != (int)PlayerSkillSourceType.EquipmentGrant)
                continue;

            if (!source.SourcePlayerItemId.HasValue || !activeEquipmentSourceIds.Contains(source.SourcePlayerItemId.Value))
            {
                await _playerSkillGrantSources.DeleteAsync(source, cancellationToken);
                changed = true;
            }
        }

        if (changed)
        {
            playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
            grantSources = await _playerSkillGrantSources.ListByPlayerIdAsync(playerId, cancellationToken);
        }

        foreach (var playerSkill in playerSkills)
        {
            var sourceCandidates = BuildCanonicalSourceCandidates(
                playerSkill,
                grantSources,
                equippedSlotByPlayerItemId);

            if (sourceCandidates.Count == 0)
            {
                if (playerSkill.SourceType == (int)PlayerSkillSourceType.EquipmentGrant)
                {
                    await RemoveLoadoutsForPlayerSkillAsync(playerSkill.Id, loadouts, cancellationToken);
                    await _playerSkills.DeleteAsync(playerSkill, cancellationToken);
                    changed = true;
                }

                continue;
            }

            var canonicalSource = ResolveCanonicalSourceCandidate(playerSkill.SkillGroupCode, sourceCandidates);
            var desiredSourcePlayerItemId = canonicalSource.IsEquipmentSource
                ? canonicalSource.SourcePlayerItemId
                : null;

            var requiresUpdate = playerSkill.SkillId != canonicalSource.SkillId ||
                                 playerSkill.SourcePlayerItemId != desiredSourcePlayerItemId ||
                                 !playerSkill.IsActive;

            if (requiresUpdate)
            {
                playerSkill.SkillId = canonicalSource.SkillId;
                playerSkill.SourcePlayerItemId = desiredSourcePlayerItemId;
                playerSkill.IsActive = true;
                playerSkill.UpdatedAt = DateTime.UtcNow;
                await _playerSkills.UpdateAsync(playerSkill, cancellationToken);
                changed = true;
            }
        }

        playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
        var snapshot = await BuildSnapshotAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);
        return new SkillEquipmentGrantSyncResult(changed, snapshot);
    }

    public async Task<EquippedSkillCastContextDto> ResolveEquippedSkillForCombatAsync(
        Guid playerId,
        int slotIndex,
        CancellationToken cancellationToken = default)
    {
        if (slotIndex < 1 || slotIndex > _gameConfig.SkillMaxLoadoutSlotCount)
            throw new GameException(MessageCode.SkillLoadoutSlotInvalid);

        var baseStats = await _characterBaseStats.GetByIdAsync(playerId, cancellationToken);
        var currentRealmTemplateId = baseStats?.RealmId;
        var playerItems = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var playerItemsById = playerItems.ToDictionary(x => x.Id);
        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, baseStats, cancellationToken);

        var loadout = loadouts.FirstOrDefault(x => x.SlotIndex == slotIndex);
        if (loadout is null)
            throw new GameException(MessageCode.SkillLoadoutSlotEmpty);

        var playerSkill = playerSkills.FirstOrDefault(x => x.Id == loadout.PlayerSkillId);
        if (playerSkill is null)
            throw new GameException(MessageCode.PlayerSkillInvalid);

        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            throw new GameException(MessageCode.SkillNotLearned);

        if (!CanAssignSkillToSlot(slotIndex, playerSkill, currentRealmTemplateId, playerItemsById, out _))
            throw new GameException(MessageCode.SkillLoadoutBlocked);

        return new EquippedSkillCastContextDto(
            playerSkill.Id,
            playerSkill.SkillId,
            slotIndex,
            playerSkill.SourcePlayerItemId,
            skillDefinition);
    }

    private async Task<OwnedSkillsSnapshotDto> BuildSnapshotAsync(
        Guid playerId,
        IReadOnlyList<PlayerSkillEntity> playerSkills,
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts,
        CharacterBaseStat? baseStats,
        CancellationToken cancellationToken)
    {
        var playerItems = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var playerItemsById = playerItems.ToDictionary(x => x.Id);

        var loadoutsBySlot = loadouts
            .Where(x => x.SlotIndex >= 1 && x.SlotIndex <= _gameConfig.SkillMaxLoadoutSlotCount)
            .GroupBy(x => x.SlotIndex)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(row => row.UpdatedAt).ThenByDescending(row => row.Id).First());

        var equippedSlotByPlayerSkillId = loadoutsBySlot.Values
            .GroupBy(x => x.PlayerSkillId)
            .ToDictionary(x => x.Key, x => x.OrderBy(row => row.SlotIndex).First().SlotIndex);

        var currentRealmTemplateId = baseStats?.RealmId;
        var skillDtos = playerSkills
            .OrderBy(x => x.SkillGroupCode)
            .ThenBy(x => x.Id)
            .Select(playerSkill => BuildPlayerSkillDto(playerSkill, equippedSlotByPlayerSkillId, currentRealmTemplateId, playerItemsById))
            .ToArray();

        var skillDtoByPlayerSkillId = skillDtos.ToDictionary(x => x.PlayerSkillId);
        var slotDtos = Enumerable.Range(1, _gameConfig.SkillMaxLoadoutSlotCount)
            .Select(slotIndex =>
            {
                if (loadoutsBySlot.TryGetValue(slotIndex, out var loadout) &&
                    skillDtoByPlayerSkillId.TryGetValue(loadout.PlayerSkillId, out var skillDto))
                {
                    return new SkillLoadoutSlotDto(slotIndex, skillDto);
                }

                return new SkillLoadoutSlotDto(slotIndex, null);
            })
            .ToArray();

        return new OwnedSkillsSnapshotDto(_gameConfig.SkillMaxLoadoutSlotCount, skillDtos, slotDtos);
    }

    private PlayerSkillDto BuildPlayerSkillDto(
        PlayerSkillEntity playerSkill,
        IReadOnlyDictionary<long, int> equippedSlotByPlayerSkillId,
        int? currentRealmTemplateId,
        IReadOnlyDictionary<long, PlayerItemEntity> playerItemsById)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            throw new InvalidOperationException($"Player skill {playerSkill.Id} references unknown skill {playerSkill.SkillId}.");

        MartialArtDefinition? martialArtDefinition = null;
        MartialArtSkillUnlockDefinition? unlockDefinition = null;
        if (playerSkill.SourceMartialArtId.HasValue && playerSkill.SourceMartialArtId.Value > 0)
            _combatDefinitions.TryGetMartialArt(playerSkill.SourceMartialArtId.Value, out martialArtDefinition);

        if (playerSkill.SourceMartialArtSkillId.HasValue && playerSkill.SourceMartialArtSkillId.Value > 0)
            _combatDefinitions.TryGetMartialArtSkill(playerSkill.SourceMartialArtSkillId.Value, out unlockDefinition);

        var availability = ResolveLoadoutAvailability(playerSkill, currentRealmTemplateId, playerItemsById);
        var equippedSlotIndex = equippedSlotByPlayerSkillId.GetValueOrDefault(playerSkill.Id);
        return new PlayerSkillDto(
            playerSkill.Id,
            skillDefinition.Id,
            skillDefinition.Code,
            skillDefinition.Name,
            skillDefinition.GroupCode,
            skillDefinition.SkillLevel,
            (int)skillDefinition.SkillType,
            (int)skillDefinition.SkillCategory,
            (int)skillDefinition.TargetType,
            skillDefinition.CastRange,
            skillDefinition.CastTimeMs,
            skillDefinition.TravelTimeMs,
            skillDefinition.CooldownMs,
            _descriptions.BuildSkillDescription(skillDefinition),
            playerSkill.SourceType,
            playerSkill.SourceMartialArtId ?? 0,
            martialArtDefinition?.Name ?? string.Empty,
            playerSkill.SourcePlayerItemId,
            unlockDefinition?.UnlockStage ?? 0,
            availability.CanAssignToLoadout,
            availability.BlockedReason,
            equippedSlotIndex > 0,
            equippedSlotIndex);
    }

    private void ValidateLoadoutChange(
        int slotIndex,
        PlayerSkillEntity playerSkill,
        int? currentRealmTemplateId,
        IReadOnlyDictionary<long, PlayerItemEntity> playerItemsById)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out _))
            throw new GameException(MessageCode.SkillNotLearned);

        if (!CanAssignSkillToSlot(slotIndex, playerSkill, currentRealmTemplateId, playerItemsById, out _))
            throw new GameException(MessageCode.SkillLoadoutBlocked);
    }

    private async Task<(IReadOnlyList<PlayerSkillEntity> Skills, IReadOnlyList<PlayerSkillLoadoutEntity> Loadouts)> NormalizeLoadoutAsync(
        Guid playerId,
        IReadOnlyList<PlayerSkillEntity> playerSkills,
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts,
        CharacterBaseStat? baseStats,
        CancellationToken cancellationToken)
    {
        var currentRealmTemplateId = baseStats?.RealmId;
        var playerItems = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var playerItemsById = playerItems.ToDictionary(x => x.Id);
        var playerSkillById = playerSkills.ToDictionary(x => x.Id);

        var invalidLoadouts = loadouts
            .Where(x =>
            {
                if (x.SlotIndex < 1 || x.SlotIndex > _gameConfig.SkillMaxLoadoutSlotCount)
                    return true;

                if (!playerSkillById.TryGetValue(x.PlayerSkillId, out var skill))
                    return true;

                return !CanAssignSkillToSlot(x.SlotIndex, skill, currentRealmTemplateId, playerItemsById, out _);
            })
            .ToArray();

        var changed = false;
        for (var i = 0; i < invalidLoadouts.Length; i++)
        {
            await _playerSkillLoadouts.DeleteAsync(invalidLoadouts[i], cancellationToken);
            changed = true;
        }

        if (changed)
            loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);

        if (!changed)
            return (playerSkills, loadouts);

        var reloadedLoadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        return (playerSkills, reloadedLoadouts);
    }

    private IEnumerable<EquipmentGrantedSkillCandidate> BuildEquipmentGrantCandidates(
        IReadOnlyDictionary<long, PlayerItemEntity> playerItemsById,
        IReadOnlyDictionary<long, int> equippedSlotByPlayerItemId)
    {
        foreach (var pair in equippedSlotByPlayerItemId)
        {
            if (!playerItemsById.TryGetValue(pair.Key, out var playerItem))
                continue;

            if (!_itemDefinitions.TryGetItem(playerItem.ItemTemplateId, out var itemDefinition) || itemDefinition.Equipment is null)
                continue;

            foreach (var grant in itemDefinition.Equipment.SkillGrants)
            {
                if (!_combatDefinitions.TryGetSkill(grant.SkillId, out var skillDefinition))
                {
                    throw new InvalidOperationException(
                        $"Equipment template {itemDefinition.Id} grants unknown skill {grant.SkillId}.");
                }

                yield return new EquipmentGrantedSkillCandidate(
                    skillDefinition.GroupCode,
                    grant.SkillId,
                    pair.Key,
                    itemDefinition.Id,
                    pair.Value,
                    skillDefinition.SkillLevel);
            }
        }
    }

    private async Task<(IReadOnlyList<PlayerSkillGrantSourceEntity> Sources, bool Changed)> EnsureNonEquipmentSourceRecordsAsync(
        IReadOnlyList<PlayerSkillEntity> playerSkills,
        IReadOnlyList<PlayerSkillGrantSourceEntity> grantSources,
        CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var playerSkill in playerSkills)
        {
            if (playerSkill.SourceType == (int)PlayerSkillSourceType.EquipmentGrant)
                continue;

            var nonEquipmentSources = grantSources
                .Where(x => x.PlayerSkillId == playerSkill.Id && x.SourceType != (int)PlayerSkillSourceType.EquipmentGrant)
                .ToArray();

            if (nonEquipmentSources.Length == 0)
            {
                var newSource = new PlayerSkillGrantSourceEntity
                {
                    PlayerId = playerSkill.PlayerId,
                    PlayerSkillId = playerSkill.Id,
                    SourceType = playerSkill.SourceType,
                    GrantedSkillId = playerSkill.SkillId,
                    SourcePlayerItemId = null,
                    SourceEquipmentTemplateId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                newSource.Id = await _playerSkillGrantSources.CreateAsync(newSource, cancellationToken);
                grantSources = grantSources.Append(newSource).ToArray();
                changed = true;
                continue;
            }

            if (playerSkill.SourcePlayerItemId.HasValue)
                continue;

            if (nonEquipmentSources.Length != 1)
                continue;

            var baselineSource = nonEquipmentSources[0];
            if (baselineSource.SourceType == playerSkill.SourceType &&
                baselineSource.GrantedSkillId != playerSkill.SkillId)
            {
                baselineSource.GrantedSkillId = playerSkill.SkillId;
                baselineSource.UpdatedAt = DateTime.UtcNow;
                await _playerSkillGrantSources.UpdateAsync(baselineSource, cancellationToken);
                changed = true;
            }
        }

        return (grantSources, changed);
    }

    private List<CanonicalSkillSourceCandidate> BuildCanonicalSourceCandidates(
        PlayerSkillEntity playerSkill,
        IReadOnlyList<PlayerSkillGrantSourceEntity> grantSources,
        IReadOnlyDictionary<long, int> equippedSlotByPlayerItemId)
    {
        var candidates = new List<CanonicalSkillSourceCandidate>();
        var sources = grantSources.Where(x => x.PlayerSkillId == playerSkill.Id).ToArray();
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            if (!_combatDefinitions.TryGetSkill(source.GrantedSkillId, out var skillDefinition))
            {
                throw new InvalidOperationException(
                    $"Player skill source {source.Id} references unknown granted skill {source.GrantedSkillId}.");
            }

            if (source.SourceType == (int)PlayerSkillSourceType.EquipmentGrant)
            {
                if (!source.SourcePlayerItemId.HasValue ||
                    !equippedSlotByPlayerItemId.TryGetValue(source.SourcePlayerItemId.Value, out var equippedSlotIndex))
                {
                    continue;
                }

                candidates.Add(new CanonicalSkillSourceCandidate(
                    source.GrantedSkillId,
                    skillDefinition.SkillLevel,
                    true,
                    source.SourceType,
                    source.SourcePlayerItemId,
                    equippedSlotIndex));
                continue;
            }

            candidates.Add(new CanonicalSkillSourceCandidate(
                source.GrantedSkillId,
                skillDefinition.SkillLevel,
                false,
                source.SourceType,
                null,
                null));
        }

        return candidates;
    }

    private CanonicalSkillSourceCandidate ResolveCanonicalSourceCandidate(
        string skillGroupCode,
        IReadOnlyList<CanonicalSkillSourceCandidate> candidates)
    {
        var maxLevel = candidates.Max(x => x.SkillLevel);
        var topCandidates = candidates
            .Where(x => x.SkillLevel == maxLevel)
            .ToArray();

        var distinctSkillIds = topCandidates
            .Select(x => x.SkillId)
            .Distinct()
            .ToArray();

        if (distinctSkillIds.Length > 1)
        {
            throw new InvalidOperationException(
                $"Skill group '{skillGroupCode}' has multiple templates at skill level {maxLevel}. " +
                "Cung group khong duoc co cung level nhung khac template.");
        }

        var chosenSkillId = distinctSkillIds[0];
        var sameSkillCandidates = topCandidates
            .Where(x => x.SkillId == chosenSkillId)
            .ToArray();

        var nonEquipmentCandidate = sameSkillCandidates
            .Where(x => !x.IsEquipmentSource)
            .OrderBy(x => x.SourceType)
            .FirstOrDefault();

        if (nonEquipmentCandidate.SkillId > 0)
            return nonEquipmentCandidate;

        return sameSkillCandidates
            .OrderBy(x => x.EquipmentSlotIndex ?? int.MaxValue)
            .ThenBy(x => x.SourcePlayerItemId ?? long.MaxValue)
            .First();
    }

    private LoadoutAvailability ResolveLoadoutAvailability(
        PlayerSkillEntity playerSkill,
        int? currentRealmTemplateId,
        IReadOnlyDictionary<long, PlayerItemEntity> playerItemsById)
    {
        if (!playerSkill.SourcePlayerItemId.HasValue || playerSkill.SourcePlayerItemId.Value <= 0)
            return LoadoutAvailability.Available;

        if (!playerItemsById.TryGetValue(playerSkill.SourcePlayerItemId.Value, out var playerItem))
            return new LoadoutAvailability(false, MissingEquipmentGrantBlockedReason);

        if (!_itemDefinitions.TryGetItem(playerItem.ItemTemplateId, out var itemDefinition) || itemDefinition.Equipment is null)
            return new LoadoutAvailability(false, MissingEquipmentGrantBlockedReason);

        var grant = itemDefinition.Equipment.SkillGrants.FirstOrDefault(x => x.SkillId == playerSkill.SkillId);
        if (grant is null)
            return new LoadoutAvailability(false, MissingEquipmentGrantBlockedReason);

        if (!grant.RequiredRealmTemplateId.HasValue || grant.RequiredRealmTemplateId.Value <= 0)
            return LoadoutAvailability.Available;

        if (currentRealmTemplateId.HasValue && currentRealmTemplateId.Value >= grant.RequiredRealmTemplateId.Value)
            return LoadoutAvailability.Available;

        return new LoadoutAvailability(false, RealmRequirementBlockedReason);
    }

    private bool CanAssignSkillToSlot(
        int slotIndex,
        PlayerSkillEntity playerSkill,
        int? currentRealmTemplateId,
        IReadOnlyDictionary<long, PlayerItemEntity> playerItemsById,
        out string blockedReason)
    {
        var availability = ResolveLoadoutAvailability(playerSkill, currentRealmTemplateId, playerItemsById);
        if (!availability.CanAssignToLoadout)
        {
            blockedReason = availability.BlockedReason ?? string.Empty;
            return false;
        }

        blockedReason = string.Empty;
        return true;
    }

    private async Task RemoveLoadoutsForPlayerSkillAsync(
        long playerSkillId,
        IReadOnlyList<PlayerSkillLoadoutEntity> currentLoadouts,
        CancellationToken cancellationToken)
    {
        var removed = currentLoadouts
            .Where(x => x.PlayerSkillId == playerSkillId)
            .ToArray();

        for (var i = 0; i < removed.Length; i++)
            await _playerSkillLoadouts.DeleteAsync(removed[i], cancellationToken);
    }

    private bool HasEquipmentSource(PlayerSkillEntity playerSkill) =>
        playerSkill.SourcePlayerItemId.HasValue && playerSkill.SourcePlayerItemId.Value > 0;

    private int GetSkillLevel(PlayerSkillEntity playerSkill)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            return 0;

        return skillDefinition.SkillLevel;
    }

    private async Task ClearSlotAsync(
        Guid playerId,
        int slotIndex,
        IReadOnlyList<PlayerSkillLoadoutEntity> currentLoadouts,
        CancellationToken cancellationToken)
    {
        var loadout = currentLoadouts.FirstOrDefault(x => x.PlayerId == playerId && x.SlotIndex == slotIndex);
        if (loadout is null)
            return;

        await _playerSkillLoadouts.DeleteAsync(loadout, cancellationToken);
    }

    private async Task RemoveDuplicateLoadoutsAsync(
        Guid playerId,
        long playerSkillId,
        IReadOnlyList<PlayerSkillLoadoutEntity> currentLoadouts,
        CancellationToken cancellationToken)
    {
        var duplicates = currentLoadouts
            .Where(x => x.PlayerId == playerId && x.PlayerSkillId == playerSkillId)
            .ToArray();

        for (var i = 0; i < duplicates.Length; i++)
            await _playerSkillLoadouts.DeleteAsync(duplicates[i], cancellationToken);
    }

    private async Task SwapLoadoutRowsAsync(
        PlayerSkillLoadoutEntity sourceLoadout,
        PlayerSkillLoadoutEntity targetLoadout,
        int sourceSlotIndex,
        int targetSlotIndex,
        int tempSlotIndex,
        CancellationToken cancellationToken)
    {
        sourceLoadout.SlotIndex = tempSlotIndex;
        sourceLoadout.UpdatedAt = DateTime.UtcNow;
        await _playerSkillLoadouts.UpdateAsync(sourceLoadout, cancellationToken);

        targetLoadout.SlotIndex = sourceSlotIndex;
        targetLoadout.UpdatedAt = DateTime.UtcNow;
        await _playerSkillLoadouts.UpdateAsync(targetLoadout, cancellationToken);

        sourceLoadout.SlotIndex = targetSlotIndex;
        sourceLoadout.UpdatedAt = DateTime.UtcNow;
        await _playerSkillLoadouts.UpdateAsync(sourceLoadout, cancellationToken);
    }

    private readonly record struct EquipmentGrantedSkillCandidate(
        string SkillGroupCode,
        int SkillId,
        long PlayerItemId,
        int EquipmentTemplateId,
        int SlotIndex,
        int SkillLevel);

    private readonly record struct CanonicalSkillSourceCandidate(
        int SkillId,
        int SkillLevel,
        bool IsEquipmentSource,
        int SourceType,
        long? SourcePlayerItemId,
        int? EquipmentSlotIndex);

    private readonly record struct LoadoutAvailability(
        bool CanAssignToLoadout,
        string? BlockedReason)
    {
        public static LoadoutAvailability Available => new(true, null);
    }
}

public readonly record struct OwnedSkillsSnapshotDto(
    int MaxLoadoutSlotCount,
    IReadOnlyList<PlayerSkillDto> Skills,
    IReadOnlyList<SkillLoadoutSlotDto> LoadoutSlots);

public readonly record struct SkillEquipmentGrantSyncResult(
    bool Changed,
    OwnedSkillsSnapshotDto Snapshot);

public readonly record struct EquippedSkillCastContextDto(
    long PlayerSkillId,
    int SkillId,
    int SkillSlotIndex,
    long? SourcePlayerItemId,
    SkillDefinition Skill);
