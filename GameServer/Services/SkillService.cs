using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameShared.Messages;
using GameShared.Enums;

namespace GameServer.Services;

public sealed class SkillService
{
    public const int DefaultMaxLoadoutSlotCount = 5;
    public const int BasicSkillSlotIndex = 1;

    private readonly CombatDefinitionCatalog _combatDefinitions;
    private readonly PlayerSkillRepository _playerSkills;
    private readonly PlayerSkillLoadoutRepository _playerSkillLoadouts;

    public SkillService(
        CombatDefinitionCatalog combatDefinitions,
        PlayerSkillRepository playerSkills,
        PlayerSkillLoadoutRepository playerSkillLoadouts)
    {
        _combatDefinitions = combatDefinitions;
        _playerSkills = playerSkills;
        _playerSkillLoadouts = playerSkillLoadouts;
    }

    public async Task<OwnedSkillsSnapshotDto> GetOwnedSkillsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, cancellationToken);
        return BuildSnapshot(playerSkills, loadouts);
    }

    public async Task<OwnedSkillsSnapshotDto> SetSkillLoadoutSlotAsync(
        Guid playerId,
        int slotIndex,
        long? playerSkillId,
        CancellationToken cancellationToken = default)
    {
        if (slotIndex < 1 || slotIndex > DefaultMaxLoadoutSlotCount)
            throw new GameException(MessageCode.SkillLoadoutSlotInvalid);

        var normalizedPlayerSkillId = playerSkillId.GetValueOrDefault();
        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, cancellationToken);

        if (normalizedPlayerSkillId <= 0)
        {
            if (slotIndex == BasicSkillSlotIndex && HasOwnedBasicSkill(playerSkills))
                throw new GameException(MessageCode.SkillLoadoutBasicSkillRequired);

            await ClearSlotAsync(playerId, slotIndex, loadouts, cancellationToken);
            loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
            (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, cancellationToken);
            return BuildSnapshot(playerSkills, loadouts);
        }

        var playerSkill = playerSkills.FirstOrDefault(x => x.Id == normalizedPlayerSkillId);
        if (playerSkill is null)
            throw new GameException(MessageCode.PlayerSkillInvalid);

        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out _))
            throw new GameException(MessageCode.SkillNotLearned);

        ValidateLoadoutChange(slotIndex, playerSkill);

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
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, cancellationToken);
        return BuildSnapshot(playerSkills, loadouts);
    }

    public async Task<EquippedSkillCastContextDto> ResolveEquippedSkillForCombatAsync(
        Guid playerId,
        int slotIndex,
        CancellationToken cancellationToken = default)
    {
        if (slotIndex < 1 || slotIndex > DefaultMaxLoadoutSlotCount)
            throw new GameException(MessageCode.SkillLoadoutSlotInvalid);

        IReadOnlyList<PlayerSkillEntity> playerSkills = await _playerSkills.ListByPlayerIdAsync(playerId, cancellationToken);
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        (playerSkills, loadouts) = await NormalizeLoadoutAsync(playerId, playerSkills, loadouts, cancellationToken);

        var loadout = loadouts.FirstOrDefault(x => x.SlotIndex == slotIndex);
        if (loadout is null)
            throw new GameException(MessageCode.SkillLoadoutSlotEmpty);

        var playerSkill = playerSkills.FirstOrDefault(x => x.Id == loadout.PlayerSkillId);
        if (playerSkill is null)
            throw new GameException(MessageCode.PlayerSkillInvalid);

        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            throw new GameException(MessageCode.SkillNotLearned);

        return new EquippedSkillCastContextDto(
            playerSkill.Id,
            playerSkill.SkillId,
            slotIndex,
            skillDefinition);
    }

    private OwnedSkillsSnapshotDto BuildSnapshot(
        IReadOnlyList<PlayerSkillEntity> playerSkills,
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts)
    {
        var loadoutsBySlot = loadouts
            .Where(x => x.SlotIndex >= 1 && x.SlotIndex <= DefaultMaxLoadoutSlotCount)
            .GroupBy(x => x.SlotIndex)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(row => row.UpdatedAt).ThenByDescending(row => row.Id).First());

        var equippedSlotByPlayerSkillId = loadoutsBySlot.Values
            .GroupBy(x => x.PlayerSkillId)
            .ToDictionary(x => x.Key, x => x.OrderBy(row => row.SlotIndex).First().SlotIndex);

        var skillDtos = playerSkills
            .OrderByDescending(GetSkillCategoryOrder)
            .ThenBy(x => x.SkillGroupCode)
            .ThenBy(x => x.Id)
            .Select(playerSkill => BuildPlayerSkillDto(playerSkill, equippedSlotByPlayerSkillId))
            .ToArray();

        var skillDtoByPlayerSkillId = skillDtos.ToDictionary(x => x.PlayerSkillId);
        var slotDtos = Enumerable.Range(1, DefaultMaxLoadoutSlotCount)
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

        return new OwnedSkillsSnapshotDto(DefaultMaxLoadoutSlotCount, skillDtos, slotDtos);
    }

    private PlayerSkillDto BuildPlayerSkillDto(
        PlayerSkillEntity playerSkill,
        IReadOnlyDictionary<long, int> equippedSlotByPlayerSkillId)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            throw new InvalidOperationException($"Player skill {playerSkill.Id} references unknown skill {playerSkill.SkillId}.");

        MartialArtDefinition? martialArtDefinition = null;
        MartialArtSkillUnlockDefinition? unlockDefinition = null;
        if (playerSkill.SourceMartialArtId.HasValue && playerSkill.SourceMartialArtId.Value > 0)
            _combatDefinitions.TryGetMartialArt(playerSkill.SourceMartialArtId.Value, out martialArtDefinition);

        if (playerSkill.SourceMartialArtSkillId.HasValue && playerSkill.SourceMartialArtSkillId.Value > 0)
            _combatDefinitions.TryGetMartialArtSkill(playerSkill.SourceMartialArtSkillId.Value, out unlockDefinition);

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
            skillDefinition.Description,
            playerSkill.SourceType,
            playerSkill.SourceMartialArtId ?? 0,
            martialArtDefinition?.Name ?? string.Empty,
            unlockDefinition?.UnlockStage ?? 0,
            equippedSlotIndex > 0,
            equippedSlotIndex);
    }

    private void ValidateLoadoutChange(int slotIndex, PlayerSkillEntity playerSkill)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            throw new GameException(MessageCode.SkillNotLearned);

        if (slotIndex == BasicSkillSlotIndex)
        {
            if (skillDefinition.SkillCategory != SkillCategory.Basic)
                throw new GameException(MessageCode.SkillLoadoutFirstSlotRequiresBasic);
        }
    }

    private async Task<(IReadOnlyList<PlayerSkillEntity> Skills, IReadOnlyList<PlayerSkillLoadoutEntity> Loadouts)> NormalizeLoadoutAsync(
        Guid playerId,
        IReadOnlyList<PlayerSkillEntity> playerSkills,
        IReadOnlyList<PlayerSkillLoadoutEntity> loadouts,
        CancellationToken cancellationToken)
    {
        var basicSkills = playerSkills
            .Where(IsBasicSkill)
            .OrderByDescending(GetSkillLevel)
            .ThenBy(x => x.Id)
            .ToArray();

        if (basicSkills.Length == 0)
            return (playerSkills, loadouts);

        var playerSkillById = playerSkills.ToDictionary(x => x.Id);
        var loadoutsBySlot = loadouts
            .Where(x => x.SlotIndex >= 1 && x.SlotIndex <= DefaultMaxLoadoutSlotCount)
            .GroupBy(x => x.SlotIndex)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(row => row.UpdatedAt).ThenByDescending(row => row.Id).First());

        var basicLoadouts = loadouts
            .Where(x => playerSkillById.TryGetValue(x.PlayerSkillId, out var skill) && IsBasicSkill(skill))
            .OrderBy(x => x.SlotIndex)
            .ThenBy(x => x.Id)
            .ToArray();

        if (loadoutsBySlot.TryGetValue(BasicSkillSlotIndex, out var slotOneLoadout) &&
            playerSkillById.TryGetValue(slotOneLoadout.PlayerSkillId, out var slotOneSkill) &&
            IsBasicSkill(slotOneSkill))
        {
            return (playerSkills, loadouts);
        }

        var preferredBasic = basicLoadouts
            .Select(x => playerSkillById[x.PlayerSkillId])
            .FirstOrDefault();

        preferredBasic ??= basicSkills[0];

        var changed = false;

        if (loadoutsBySlot.TryGetValue(BasicSkillSlotIndex, out var firstSlotLoadout))
        {
            if (firstSlotLoadout.PlayerSkillId != preferredBasic.Id)
            {
                firstSlotLoadout.PlayerSkillId = preferredBasic.Id;
                firstSlotLoadout.UpdatedAt = DateTime.UtcNow;
                await _playerSkillLoadouts.UpdateAsync(firstSlotLoadout, cancellationToken);
                changed = true;
            }
        }
        else
        {
            var newLoadout = new PlayerSkillLoadoutEntity
            {
                PlayerId = playerId,
                SlotIndex = BasicSkillSlotIndex,
                PlayerSkillId = preferredBasic.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _playerSkillLoadouts.CreateAsync(newLoadout, cancellationToken);
            changed = true;
        }

        foreach (var loadout in basicLoadouts)
        {
            if (loadout.PlayerSkillId != preferredBasic.Id || loadout.SlotIndex == BasicSkillSlotIndex)
                continue;

            await _playerSkillLoadouts.DeleteAsync(loadout, cancellationToken);
            changed = true;
        }

        if (!changed)
            return (playerSkills, loadouts);

        var reloadedLoadouts = await _playerSkillLoadouts.ListByPlayerIdAsync(playerId, cancellationToken);
        return (playerSkills, reloadedLoadouts);
    }

    private bool HasOwnedBasicSkill(IReadOnlyList<PlayerSkillEntity> playerSkills) =>
        playerSkills.Any(IsBasicSkill);

    private bool IsBasicSkill(PlayerSkillEntity playerSkill)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            return false;

        return skillDefinition.SkillCategory == SkillCategory.Basic;
    }

    private int GetSkillLevel(PlayerSkillEntity playerSkill)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            return 0;

        return skillDefinition.SkillLevel;
    }

    private int GetSkillCategoryOrder(PlayerSkillEntity playerSkill)
    {
        if (!_combatDefinitions.TryGetSkill(playerSkill.SkillId, out var skillDefinition))
            return 0;

        return skillDefinition.SkillCategory == SkillCategory.Basic ? 1 : 0;
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
}

public readonly record struct OwnedSkillsSnapshotDto(
    int MaxLoadoutSlotCount,
    IReadOnlyList<PlayerSkillDto> Skills,
    IReadOnlyList<SkillLoadoutSlotDto> LoadoutSlots);

public readonly record struct EquippedSkillCastContextDto(
    long PlayerSkillId,
    int SkillId,
    int SkillSlotIndex,
    SkillDefinition Skill);
