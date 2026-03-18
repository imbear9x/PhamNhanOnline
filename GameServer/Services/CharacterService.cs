using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.Time;
using GameServer.World;
using GameShared.Messages;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Services;

public sealed class CharacterService
{
    private const int DefaultRealmTemplateId = 1;
    private const int DefaultCurrentStateCode = 0;
    private const int DefaultRealmLifespan = 120;
    private const int DefaultBaseHp = 100;
    private const int DefaultBaseMp = 100;
    private const int DefaultBaseAttack = 10;
    private const int DefaultBaseSpeed = 10;
    private const int DefaultBaseSpiritualSense = 10;
    private const int DefaultBaseStamina = 100;
    private const int DefaultLifespanBonus = 0;
    private const double DefaultBaseFortune = 0.01;
    private const int DefaultBasePotential = 0;
    private const int DefaultUnallocatedPotential = 0;
    private const int DefaultAppearanceValue = 1;
    private const int DefaultHomeGardenPlotCount = 8;

    private readonly GameDb _db;
    private readonly CharacterRepository _characters;
    private readonly CharacterBaseStatRepository _baseStats;
    private readonly CharacterCurrentStateRepository _currentStates;
    private readonly PlayerCaveRepository _playerCaves;
    private readonly PlayerGardenPlotRepository _playerGardenPlots;
    private readonly RealmTemplateRepository _realmTemplates;
    private readonly GameTimeService _gameTimeService;
    private readonly MapCatalog _mapCatalog;
    private readonly CharacterBaseStatsComposer _baseStatsComposer;
    private readonly PotentialStatCatalog _potentialStatCatalog;

    public CharacterService(
        GameDb db,
        CharacterRepository characters,
        CharacterBaseStatRepository baseStats,
        CharacterCurrentStateRepository currentStates,
        PlayerCaveRepository playerCaves,
        PlayerGardenPlotRepository playerGardenPlots,
        RealmTemplateRepository realmTemplates,
        GameTimeService gameTimeService,
        MapCatalog mapCatalog,
        CharacterBaseStatsComposer baseStatsComposer,
        PotentialStatCatalog potentialStatCatalog)
    {
        _db = db;
        _characters = characters;
        _baseStats = baseStats;
        _currentStates = currentStates;
        _playerCaves = playerCaves;
        _playerGardenPlots = playerGardenPlots;
        _realmTemplates = realmTemplates;
        _gameTimeService = gameTimeService;
        _mapCatalog = mapCatalog;
        _baseStatsComposer = baseStatsComposer;
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
        await tx.CommitAsync(cancellationToken);

        var baseStatsDto = AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(baseStat, realmLifespan)));
        return new CharacterSnapshotDto(
            CharacterDto.FromEntity(character),
            baseStatsDto,
            CharacterCurrentStateDto.FromEntity(currentState));
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
        var baseStatsDto = row.BaseStats is null ? null : AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan)));
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
        var baseStatsDto = row.BaseStats is null ? null : AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan)));
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
            var baseStatsDto = row.BaseStats is null ? null : AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan)));
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
            return AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(existing, existingRealmLifespan)));
        }

        var entity = BuildDefaultCharacterBaseStats(characterId);

        await _baseStats.CreateAsync(entity, cancellationToken);
        var realmLifespan = await GetRealmLifespanAsync(entity.RealmId, cancellationToken);
        return AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(entity, realmLifespan)));
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
        existing.BaseHp = stats.RawBaseHp ?? stats.BaseHp;
        existing.BaseMp = stats.RawBaseMp ?? stats.BaseMp;
        existing.BaseAttack = stats.RawBaseAttack ?? stats.BaseAttack;
        existing.BaseSpeed = stats.RawBaseSpeed ?? stats.BaseSpeed;
        existing.BaseSpiritualSense = stats.RawBaseSpiritualSense ?? stats.BaseSpiritualSense;
        existing.BaseStamina = stats.RawBaseStamina ?? stats.BaseStamina;
        existing.LifespanBonus = stats.LifespanBonus;
        existing.BaseFortune = stats.RawBaseFortune ?? stats.BaseFortune;
        existing.BasePotential = stats.BasePotential;
        existing.UnallocatedPotential = stats.UnallocatedPotential;
        existing.BonusHp = stats.BonusHp;
        existing.BonusMp = stats.BonusMp;
        existing.BonusAttack = stats.BonusAttack;
        existing.BonusSpeed = stats.BonusSpeed;
        existing.BonusSpiritualSense = stats.BonusSpiritualSense;
        existing.BonusFortune = stats.BonusFortune;
        existing.HpUpgradeCount = stats.HpUpgradeCount;
        existing.MpUpgradeCount = stats.MpUpgradeCount;
        existing.AttackUpgradeCount = stats.AttackUpgradeCount;
        existing.SpeedUpgradeCount = stats.SpeedUpgradeCount;
        existing.SpiritualSenseUpgradeCount = stats.SpiritualSenseUpgradeCount;
        existing.FortuneUpgradeCount = stats.FortuneUpgradeCount;
        existing.CultivationProgress = stats.CultivationProgress;
        existing.PotentialRewardLocked = stats.PotentialRewardLocked;

        await _baseStats.UpdateAsync(existing, cancellationToken);
        var realmLifespan = await GetRealmLifespanAsync(existing.RealmId, cancellationToken);
        return AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(existing, realmLifespan)));
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
        var effectiveBaseStats = AttachPotentialPreviews(_baseStatsComposer.Compose(CharacterBaseStatsDto.FromEntity(
            baseStat ?? BuildDefaultCharacterBaseStats(characterId),
            realmLifespan ?? DefaultRealmLifespan)));
        var lifespanEndGameMinute = CharacterLifespanRules.CreateLifespanEndGameMinute(
            effectiveBaseStats,
            gameTime,
            DefaultRealmLifespan);
        var homeDefinition = _mapCatalog.ResolveHomeDefinition();

        return new CharacterCurrentState
        {
            CharacterId = characterId,
            CurrentHp = effectiveBaseStats.BaseHp ?? DefaultBaseHp,
            CurrentMp = effectiveBaseStats.BaseMp ?? DefaultBaseMp,
            CurrentStamina = effectiveBaseStats.BaseStamina ?? DefaultBaseStamina,
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

    private static CharacterBaseStat BuildDefaultCharacterBaseStats(Guid characterId)
    {
        return new CharacterBaseStat
        {
            CharacterId = characterId,
            RealmId = DefaultRealmTemplateId,
            Cultivation = 0,
            BaseHp = DefaultBaseHp,
            BaseMp = DefaultBaseMp,
            BaseAttack = DefaultBaseAttack,
            BaseSpeed = DefaultBaseSpeed,
            BaseSpiritualSense = DefaultBaseSpiritualSense,
            BaseStamina = DefaultBaseStamina,
            LifespanBonus = DefaultLifespanBonus,
            BaseFortune = DefaultBaseFortune,
            BasePotential = DefaultBasePotential,
            UnallocatedPotential = DefaultUnallocatedPotential,
            BonusHp = 0,
            BonusMp = 0,
            BonusAttack = 0,
            BonusSpeed = 0,
            BonusSpiritualSense = 0,
            BonusFortune = 0,
            HpUpgradeCount = 0,
            MpUpgradeCount = 0,
            AttackUpgradeCount = 0,
            SpeedUpgradeCount = 0,
            SpiritualSenseUpgradeCount = 0,
            FortuneUpgradeCount = 0,
            CultivationProgress = 0m,
            PotentialRewardLocked = false
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

    private CharacterBaseStatsDto AttachPotentialPreviews(CharacterBaseStatsDto baseStats)
    {
        return _potentialStatCatalog.AttachPreviews(baseStats);
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
}

public sealed record CharacterSnapshotDto(
    CharacterDto Character,
    CharacterBaseStatsDto? BaseStats,
    CharacterCurrentStateDto? CurrentState);
