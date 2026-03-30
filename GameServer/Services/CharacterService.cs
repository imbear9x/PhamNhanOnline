using GameServer.Config;
using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.Time;
using GameServer.World;
using GameShared.Enums;
using GameShared.Messages;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Services;

public sealed class CharacterService
{
    private const int DefaultCurrentStateCode = 0;
    private const int DefaultAppearanceValue = 1;
    private const int DefaultHomeGardenPlotCount = 8;
    private const int DefaultStarterBasicSkillId = 0;
    private const int DefaultStarterSkillSlotIndex = 1;

    private readonly GameDb _db;
    private readonly CharacterRepository _characters;
    private readonly CharacterBaseStatRepository _baseStats;
    private readonly CharacterCurrentStateRepository _currentStates;
    private readonly CharacterCreateConfig _characterCreateConfig;
    private readonly PlayerCaveRepository _playerCaves;
    private readonly PlayerGardenPlotRepository _playerGardenPlots;
    private readonly RealmTemplateRepository _realmTemplates;
    private readonly GameTimeService _gameTimeService;
    private readonly MapCatalog _mapCatalog;
    private readonly PotentialStatCatalog _potentialStatCatalog;

    public CharacterService(
        GameDb db,
        CharacterRepository characters,
        CharacterBaseStatRepository baseStats,
        CharacterCurrentStateRepository currentStates,
        CharacterCreateConfig characterCreateConfig,
        PlayerCaveRepository playerCaves,
        PlayerGardenPlotRepository playerGardenPlots,
        RealmTemplateRepository realmTemplates,
        GameTimeService gameTimeService,
        MapCatalog mapCatalog,
        PotentialStatCatalog potentialStatCatalog)
    {
        _db = db;
        _characters = characters;
        _baseStats = baseStats;
        _currentStates = currentStates;
        _characterCreateConfig = characterCreateConfig;
        _playerCaves = playerCaves;
        _playerGardenPlots = playerGardenPlots;
        _realmTemplates = realmTemplates;
        _gameTimeService = gameTimeService;
        _mapCatalog = mapCatalog;
        _potentialStatCatalog = potentialStatCatalog;
    }

    public async Task<List<CharacterDto>> GetCharactersByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var entities = await _characters.ListByAccountAsync(accountId, cancellationToken);
        return entities.Select(CharacterDto.FromEntity).ToList();
    }

    public Task<bool> IsCharacterNameUniqueAsync(string name, CancellationToken cancellationToken = default) =>
        IsCharacterNameUniqueInternalAsync(name, cancellationToken);

    public async Task<CharacterSnapshotDto> CreateCharacterAsync(
        Guid accountId,
        string name,
        int serverId,
        int modelId,
        CancellationToken cancellationToken = default)
    {
        name = NormalizeCharacterName(name);

        var existing = await _characters.ListByAccountAsync(accountId, cancellationToken);
        if (existing.Count != 0)
            throw new GameException(MessageCode.CharacterAlreadyExists);

        if (!await IsCharacterNameUniqueInternalAsync(name, cancellationToken))
            throw new GameException(MessageCode.CharacterNameAlreadyExists);

        var character = new Character
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ServerId = serverId,
            Name = name,
            ModelId = modelId,
            Gender = DefaultAppearanceValue,
            HairColor = DefaultAppearanceValue,
            EyeColor = DefaultAppearanceValue,
            FaceId = DefaultAppearanceValue,
            CreatedAt = DateTime.UtcNow,
        };

        var baseStat = BuildDefaultCharacterBaseStats(character.Id);
        var realmLifespan = await GetRealmLifespanAsync(baseStat.RealmId, cancellationToken);
        var currentState = BuildDefaultCharacterCurrentState(
            character.Id,
            baseStat,
            realmLifespan,
            _gameTimeService.GetCurrentSnapshot());

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _characters.CreateAsync(character, cancellationToken);
        await _baseStats.CreateAsync(baseStat, cancellationToken);
        await _currentStates.CreateAsync(currentState, cancellationToken);
        await EnsureHomeCaveAsync(character.Id, cancellationToken);
        await InitializeCharacterStarterResourcesAsync(character.Id, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var baseStatsDto = await EnrichBaseStatsAsync(CharacterBaseStatsDto.FromEntity(baseStat, realmLifespan), cancellationToken);
        return new CharacterSnapshotDto(
            CharacterDto.FromEntity(character),
            baseStatsDto,
            CharacterCurrentStateDto.FromEntity(currentState));
    }

    public async Task InitializeCharacterStarterResourcesAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStarterBasicSkillAsync(characterId, cancellationToken);
    }

    public async Task<CharacterSnapshotDto?> LoadCharacterSnapshotByAccountAsync(
        Guid accountId,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from c in _db.GetTable<Character>()
            where c.Id == characterId && c.AccountId == accountId
            from b in _db.GetTable<CharacterBaseStat>().LeftJoin(baseStats => baseStats.CharacterId == c.Id)
            from s in _db.GetTable<CharacterCurrentState>().LeftJoin(currentState => currentState.CharacterId == c.Id)
            select new { Character = c, BaseStats = b, CurrentState = s };

        var row = await query.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var realmLifespan = row.BaseStats is null
            ? null
            : await GetRealmLifespanAsync(row.BaseStats.RealmId, cancellationToken);
        var characterDto = CharacterDto.FromEntity(row.Character);
        var baseStatsDto = row.BaseStats is null
            ? null
            : await EnrichBaseStatsAsync(CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan), cancellationToken);
        var currentStateDto = row.CurrentState is null ? null : CharacterCurrentStateDto.FromEntity(row.CurrentState);
        return new CharacterSnapshotDto(characterDto, baseStatsDto, currentStateDto);
    }

    public async Task<CharacterSnapshotDto?> LoadCharacterSnapshotAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        var query =
            from c in _db.GetTable<Character>()
            where c.Id == characterId
            from b in _db.GetTable<CharacterBaseStat>().LeftJoin(baseStats => baseStats.CharacterId == c.Id)
            from s in _db.GetTable<CharacterCurrentState>().LeftJoin(currentState => currentState.CharacterId == c.Id)
            select new { Character = c, BaseStats = b, CurrentState = s };

        var row = await query.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var realmLifespan = row.BaseStats is null
            ? null
            : await GetRealmLifespanAsync(row.BaseStats.RealmId, cancellationToken);
        var characterDto = CharacterDto.FromEntity(row.Character);
        var baseStatsDto = row.BaseStats is null
            ? null
            : await EnrichBaseStatsAsync(CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan), cancellationToken);
        var currentStateDto = row.CurrentState is null ? null : CharacterCurrentStateDto.FromEntity(row.CurrentState);
        return new CharacterSnapshotDto(characterDto, baseStatsDto, currentStateDto);
    }

    public async Task<List<CharacterSnapshotDto>> ListCultivatingCharacterSnapshotsAsync(
        IReadOnlyCollection<Guid>? excludeCharacterIds = null,
        CancellationToken cancellationToken = default)
    {
        var excluded = excludeCharacterIds?.ToHashSet() ?? [];
        var query =
            from c in _db.GetTable<Character>()
            from b in _db.GetTable<CharacterBaseStat>().LeftJoin(baseStats => baseStats.CharacterId == c.Id)
            from s in _db.GetTable<CharacterCurrentState>().LeftJoin(currentState => currentState.CharacterId == c.Id)
            where s != null && s.CurrentState == CharacterRuntimeStateCodes.Cultivating
            select new { Character = c, BaseStats = b, CurrentState = s };

        var rows = await query.ToListAsync(cancellationToken);
        var result = new List<CharacterSnapshotDto>(rows.Count);
        foreach (var row in rows)
        {
            if (excluded.Contains(row.Character.Id))
                continue;

            var realmLifespan = row.BaseStats is null
                ? null
                : await GetRealmLifespanAsync(row.BaseStats.RealmId, cancellationToken);
            var characterDto = CharacterDto.FromEntity(row.Character);
            var baseStatsDto = row.BaseStats is null
                ? null
                : await EnrichBaseStatsAsync(CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan), cancellationToken);
            var currentStateDto = row.CurrentState is null ? null : CharacterCurrentStateDto.FromEntity(row.CurrentState);
            result.Add(new CharacterSnapshotDto(characterDto, baseStatsDto, currentStateDto));
        }

        return result;
    }

    public async Task<CharacterDto> UpdateCharacterAsync(
        Guid characterId,
        string name,
        int modelId,
        int gender,
        int hairColor,
        int eyeColor,
        int faceId,
        CancellationToken cancellationToken = default)
    {
        name = NormalizeCharacterName(name);

        var entity = await _characters.GetByIdAsync(characterId, cancellationToken);
        if (entity is null)
            throw new GameException(MessageCode.CharacterNotFound);

        if (!string.Equals(entity.Name, name, StringComparison.Ordinal))
        {
            if (!await IsCharacterNameUniqueInternalAsync(name, cancellationToken))
                throw new GameException(MessageCode.CharacterNameAlreadyExists);
            entity.Name = name;
        }

        entity.ModelId = modelId;
        entity.Gender = gender;
        entity.HairColor = hairColor;
        entity.EyeColor = eyeColor;
        entity.FaceId = faceId;

        await _characters.UpdateAsync(entity, cancellationToken);
        return CharacterDto.FromEntity(entity);
    }

    public async Task<CharacterBaseStatsDto> InitializeCharacterBaseStatsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var character = await _characters.GetByIdAsync(characterId, cancellationToken);
        if (character is null)
            throw new GameException(MessageCode.CharacterNotFound);

        var existing = await _baseStats.GetByIdAsync(characterId, cancellationToken);
        if (existing is not null)
        {
            var existingRealmLifespan = await GetRealmLifespanAsync(existing.RealmId, cancellationToken);
            return await EnrichBaseStatsAsync(CharacterBaseStatsDto.FromEntity(existing, existingRealmLifespan), cancellationToken);
        }

        var entity = BuildDefaultCharacterBaseStats(characterId);

        await _baseStats.CreateAsync(entity, cancellationToken);
        var realmLifespan = await GetRealmLifespanAsync(entity.RealmId, cancellationToken);
        return await EnrichBaseStatsAsync(CharacterBaseStatsDto.FromEntity(entity, realmLifespan), cancellationToken);
    }

    public async Task<CharacterBaseStatsDto> UpdateCharacterBaseStatsAsync(
        CharacterBaseStatsDto stats,
        CancellationToken cancellationToken = default)
    {
        var existing = await _baseStats.GetByIdAsync(stats.CharacterId, cancellationToken);
        if (existing is null)
            throw new GameException(MessageCode.CharacterNotFound);

        existing.RealmId = stats.RealmTemplateId;
        existing.Cultivation = stats.Cultivation;
        existing.BaseHp = stats.BaseHp;
        existing.BaseMp = stats.BaseMp;
        existing.BaseAttack = stats.BaseAttack;
        existing.BaseMoveSpeed = stats.BaseMoveSpeed.HasValue ? (decimal)stats.BaseMoveSpeed.Value : null;
        existing.BaseSpeed = stats.BaseSpeed;
        existing.BaseSpiritualSense = stats.BaseSpiritualSense;
        existing.BaseStamina = stats.BaseStamina;
        existing.LifespanBonus = stats.LifespanBonus;
        existing.BaseFortune = stats.BaseFortune;
        existing.BasePotential = stats.BasePotential;
        existing.UnallocatedPotential = stats.UnallocatedPotential;
        existing.HpUpgradeCount = stats.HpUpgradeCount;
        existing.MpUpgradeCount = stats.MpUpgradeCount;
        existing.AttackUpgradeCount = stats.AttackUpgradeCount;
        existing.SpeedUpgradeCount = stats.SpeedUpgradeCount;
        existing.SpiritualSenseUpgradeCount = stats.SpiritualSenseUpgradeCount;
        existing.FortuneUpgradeCount = stats.FortuneUpgradeCount;
        existing.ActiveMartialArtId = stats.ActiveMartialArtId;
        existing.CultivationProgress = stats.CultivationProgress;
        existing.PotentialRewardLocked = stats.PotentialRewardLocked;

        await _baseStats.UpdateAsync(existing, cancellationToken);
        var realmLifespan = await GetRealmLifespanAsync(existing.RealmId, cancellationToken);
        return await EnrichBaseStatsAsync(CharacterBaseStatsDto.FromEntity(existing, realmLifespan), cancellationToken);
    }

    public async Task<CharacterCurrentStateDto?> GetCharacterCurrentStateAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _currentStates.GetByIdAsync(characterId, cancellationToken);
        return entity is null ? null : CharacterCurrentStateDto.FromEntity(entity);
    }

    public async Task<CharacterCurrentStateDto> CreateCharacterCurrentStateAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var character = await _characters.GetByIdAsync(characterId, cancellationToken);
        if (character is null)
            throw new GameException(MessageCode.CharacterNotFound);

        var existing = await _currentStates.GetByIdAsync(characterId, cancellationToken);
        if (existing is not null)
            return CharacterCurrentStateDto.FromEntity(existing);

        var baseStats = await _baseStats.GetByIdAsync(characterId, cancellationToken);
        var realmLifespan = await GetRealmLifespanAsync(baseStats?.RealmId, cancellationToken);
        var entity = BuildDefaultCharacterCurrentState(
            characterId,
            baseStats,
            realmLifespan,
            _gameTimeService.GetCurrentSnapshot());
        await _currentStates.CreateAsync(entity, cancellationToken);
        return CharacterCurrentStateDto.FromEntity(entity);
    }

    public async Task<CharacterCurrentStateDto> UpdateCharacterCurrentStateAsync(
        CharacterCurrentStateDto state,
        CancellationToken cancellationToken = default)
    {
        var existing = await _currentStates.GetByIdAsync(state.CharacterId, cancellationToken);
        if (existing is null)
            throw new GameException(MessageCode.CharacterNotFound);

        existing.CurrentHp = state.CurrentHp;
        existing.CurrentMp = state.CurrentMp;
        existing.CurrentStamina = state.CurrentStamina;
        existing.LifespanEndGameMinute = state.LifespanEndGameMinute;
        existing.CurrentMapId = state.CurrentMapId;
        existing.CurrentZoneIndex = state.CurrentZoneIndex;
        existing.CurrentPosX = state.CurrentPosX;
        existing.CurrentPosY = state.CurrentPosY;
        existing.IsDead = state.IsDead;
        existing.CurrentState = state.CurrentState;
        existing.CultivationStartedAtUtc = NormalizeUtcNullable(state.CultivationStartedAtUtc);
        existing.LastCultivationRewardedAtUtc = NormalizeUtcNullable(state.LastCultivationRewardedAtUtc);
        existing.LastSavedAt = NormalizeUtc(state.LastSavedAt);

        await _currentStates.UpdateAsync(existing, cancellationToken);
        return CharacterCurrentStateDto.FromEntity(existing);
    }

    public async Task<CharacterSnapshotDto> UpdateCharacterCultivationAsync(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState,
        CancellationToken cancellationToken = default)
    {
        return await UpdateCharacterRuntimeSnapshotAsync(baseStats, currentState, cancellationToken);
    }

    public async Task<CharacterSnapshotDto> UpdateCharacterRuntimeSnapshotAsync(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var updatedBaseStats = await UpdateCharacterBaseStatsAsync(baseStats, cancellationToken);
        var updatedCurrentState = await UpdateCharacterCurrentStateAsync(currentState, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new CharacterSnapshotDto(
            (await LoadCharacterSnapshotAsync(baseStats.CharacterId, cancellationToken))!.Character,
            updatedBaseStats,
            updatedCurrentState);
    }

    private async Task<bool> IsCharacterNameUniqueInternalAsync(string name, CancellationToken cancellationToken)
    {
        name = NormalizeCharacterName(name);
        return !await _characters.NameExistsAsync(name, cancellationToken);
    }

    private CharacterCurrentState BuildDefaultCharacterCurrentState(
        Guid characterId,
        CharacterBaseStat? baseStat,
        int? realmLifespan,
        GameTimeSnapshot gameTime)
    {
        var effectiveBaseStats = _potentialStatCatalog.AttachPreviews(CharacterBaseStatsDto.FromEntity(
            baseStat ?? BuildDefaultCharacterBaseStats(characterId),
            realmLifespan ?? _characterCreateConfig.FallbackRealmLifespan));
        var lifespanEndGameMinute = CharacterLifespanRules.CreateLifespanEndGameMinute(
            effectiveBaseStats,
            gameTime,
            _characterCreateConfig.FallbackRealmLifespan);
        var homeDefinition = _mapCatalog.ResolveHomeDefinition();

        return new CharacterCurrentState
        {
            CharacterId = characterId,
            CurrentHp = effectiveBaseStats.GetEffectiveHp(),
            CurrentMp = effectiveBaseStats.GetEffectiveMp(),
            CurrentStamina = effectiveBaseStats.GetEffectiveStamina(),
            LifespanEndGameMinute = lifespanEndGameMinute,
            CurrentMapId = homeDefinition.MapId,
            CurrentZoneIndex = homeDefinition.DefaultZoneIndex,
            CurrentPosX = homeDefinition.DefaultSpawnPosition.X,
            CurrentPosY = homeDefinition.DefaultSpawnPosition.Y,
            IsDead = false,
            CurrentState = DefaultCurrentStateCode,
            CultivationStartedAtUtc = null,
            LastCultivationRewardedAtUtc = null,
            LastSavedAt = DateTime.UtcNow
        };
    }

    private CharacterBaseStat BuildDefaultCharacterBaseStats(Guid characterId)
    {
        return new CharacterBaseStat
        {
            CharacterId = characterId,
            RealmId = _characterCreateConfig.RealmTemplateId,
            Cultivation = _characterCreateConfig.Cultivation,
            BaseHp = _characterCreateConfig.BaseHp,
            BaseMp = _characterCreateConfig.BaseMp,
            BaseAttack = _characterCreateConfig.BaseAttack,
            BaseMoveSpeed = _characterCreateConfig.BaseMoveSpeed,
            BaseSpeed = _characterCreateConfig.BaseSpeed,
            BaseSpiritualSense = _characterCreateConfig.BaseSpiritualSense,
            BaseStamina = _characterCreateConfig.BaseStamina,
            LifespanBonus = _characterCreateConfig.LifespanBonus,
            BaseFortune = _characterCreateConfig.BaseFortune,
            BasePotential = _characterCreateConfig.BasePotential,
            UnallocatedPotential = _characterCreateConfig.UnallocatedPotential,
            HpUpgradeCount = 0,
            MpUpgradeCount = 0,
            AttackUpgradeCount = 0,
            SpeedUpgradeCount = 0,
            SpiritualSenseUpgradeCount = 0,
            FortuneUpgradeCount = 0,
            ActiveMartialArtId = null,
            CultivationProgress = 0m,
            PotentialRewardLocked = _characterCreateConfig.PotentialRewardLocked
        };
    }

    private async Task<int?> GetRealmLifespanAsync(int? realmTemplateId, CancellationToken cancellationToken)
    {
        if (!realmTemplateId.HasValue)
            return null;

        var realm = await _realmTemplates.GetByIdAsync(realmTemplateId.Value, cancellationToken);
        return realm?.Lifespan;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? NormalizeUtcNullable(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return NormalizeUtc(value.Value);
    }

    private static string NormalizeCharacterName(string name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length is < 3 or > 20)
            throw new GameException(MessageCode.CharacterNameInvalid);

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            var ok = char.IsLetterOrDigit(ch) || ch == ' ' || ch == '_';
            if (!ok)
                throw new GameException(MessageCode.CharacterNameInvalid);
        }

        if (!name.Any(char.IsLetterOrDigit))
            throw new GameException(MessageCode.CharacterNameInvalid);

        return name;
    }

    public async Task<CharacterBaseStatsDto> EnrichBaseStatsAsync(
        CharacterBaseStatsDto baseStats,
        CancellationToken cancellationToken = default)
    {
        var enriched = _potentialStatCatalog.AttachPreviews(baseStats);
        if (!enriched.RealmTemplateId.HasValue || enriched.RealmTemplateId.Value <= 0)
        {
            return enriched with
            {
                RealmDisplayName = string.Empty,
                RealmMaxCultivation = 0,
                BreakthroughChancePercent = 0d,
                HasNextRealm = false
            };
        }

        var realm = await _realmTemplates.GetByIdAsync(enriched.RealmTemplateId.Value, cancellationToken);
        if (realm is null)
        {
            return enriched with
            {
                RealmDisplayName = string.Empty,
                RealmMaxCultivation = 0,
                BreakthroughChancePercent = 0d,
                HasNextRealm = false
            };
        }

        var nextRealm = await _realmTemplates.GetByIdAsync(realm.Id + 1, cancellationToken);
        return enriched with
        {
            RealmDisplayName = BuildRealmDisplayName(realm),
            RealmMaxCultivation = Math.Max(0L, realm.MaxCultivation ?? 0L),
            BreakthroughChancePercent = ResolveBreakthroughChancePercent(realm.BaseBreakthroughRate),
            HasNextRealm = nextRealm is not null
        };
    }

    private static string BuildRealmDisplayName(RealmTemplate realm)
    {
        var name = string.IsNullOrWhiteSpace(realm.Name) ? string.Empty : realm.Name.Trim();
        var stageName = string.IsNullOrWhiteSpace(realm.StageName) ? string.Empty : realm.StageName.Trim();

        if (string.IsNullOrEmpty(name))
            return stageName;
        if (string.IsNullOrEmpty(stageName))
            return name;

        return string.Concat(name, " - ", stageName);
    }

    private static double ResolveBreakthroughChancePercent(double? rawRate)
    {
        return NormalizePercentLikeRatio(rawRate) * 100d;
    }

    private static double NormalizePercentLikeRatio(double? rawValue)
    {
        if (!rawValue.HasValue || rawValue.Value <= 0d)
            return 0d;

        var value = rawValue.Value;
        if (value > 1d)
            value /= 100d;

        return Math.Clamp(value, 0d, 1d);
    }

    private async Task EnsureHomeCaveAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var existingHomeCave = await _playerCaves.GetHomeByOwnerAsync(characterId, cancellationToken);
        if (existingHomeCave is not null)
            return;

        var homeDefinition = _mapCatalog.ResolveHomeDefinition();
        var cave = new PlayerCaveEntity
        {
            OwnerCharacterId = characterId,
            MapTemplateId = homeDefinition.MapId,
            ZoneIndex = homeDefinition.DefaultZoneIndex,
            IsHome = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        cave.Id = await _playerCaves.CreateAsync(cave, cancellationToken);
        for (var plotIndex = 1; plotIndex <= DefaultHomeGardenPlotCount; plotIndex++)
        {
            await _playerGardenPlots.CreateAsync(new PlayerGardenPlotEntity
            {
                PlayerId = characterId,
                CaveId = cave.Id,
                PlotIndex = plotIndex,
                CurrentSoilPlayerItemId = null,
                CurrentPlayerHerbId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
    }

    private async Task EnsureStarterBasicSkillAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var starterSkill = await _db.GetTable<SkillEntity>()
            .FirstOrDefaultAsync(x => x.Id == DefaultStarterBasicSkillId, cancellationToken);

        if (starterSkill is null)
            throw new InvalidOperationException(
                $"Starter basic skill {DefaultStarterBasicSkillId} is missing from public.skills.");

        if (starterSkill.SkillCategory != (int)SkillCategory.Basic)
            throw new InvalidOperationException(
                $"Starter skill {DefaultStarterBasicSkillId} must have skill_category = {(int)SkillCategory.Basic}.");

        var playerSkill = await _db.GetTable<PlayerSkillEntity>()
            .FirstOrDefaultAsync(
                x => x.PlayerId == characterId && x.SkillGroupCode == starterSkill.SkillGroupCode,
                cancellationToken);

        if (playerSkill is null)
        {
            playerSkill = new PlayerSkillEntity
            {
                PlayerId = characterId,
                SkillId = starterSkill.Id,
                SkillGroupCode = starterSkill.SkillGroupCode,
                SourceType = (int)PlayerSkillSourceType.SystemGrant,
                SourceMartialArtId = null,
                SourceMartialArtSkillId = null,
                UnlockedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            playerSkill.Id = await _db.InsertEntityWithInt64IdentityAsync(playerSkill, cancellationToken);
        }

        var slotOneLoadout = await _db.GetTable<PlayerSkillLoadoutEntity>()
            .FirstOrDefaultAsync(
                x => x.PlayerId == characterId && x.SlotIndex == DefaultStarterSkillSlotIndex,
                cancellationToken);

        if (slotOneLoadout is null)
        {
            slotOneLoadout = new PlayerSkillLoadoutEntity
            {
                PlayerId = characterId,
                SlotIndex = DefaultStarterSkillSlotIndex,
                PlayerSkillId = playerSkill.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _db.InsertEntityWithInt64IdentityAsync(slotOneLoadout, cancellationToken);
            return;
        }

        if (slotOneLoadout.PlayerSkillId == playerSkill.Id)
            return;

        slotOneLoadout.PlayerSkillId = playerSkill.Id;
        slotOneLoadout.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(slotOneLoadout, token: cancellationToken);
    }
}

public sealed record CharacterSnapshotDto(
    CharacterDto Character,
    CharacterBaseStatsDto? BaseStats,
    CharacterCurrentStateDto? CurrentState);
