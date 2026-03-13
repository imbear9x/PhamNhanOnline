using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
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
    private const int DefaultBasePhysique = 10;
    private const int DefaultBaseAttack = 10;
    private const int DefaultBaseSpeed = 10;
    private const int DefaultBaseSpiritualSense = 10;
    private const int DefaultBaseStamina = 100;
    private const int DefaultLifespanBonus = 0;
    private const double DefaultBaseFortune = 0.01;
    private const int DefaultBasePotential = 0;

    private readonly GameDb _db;
    private readonly CharacterRepository _characters;
    private readonly CharacterBaseStatRepository _baseStats;
    private readonly CharacterCurrentStateRepository _currentStates;
    private readonly RealmTemplateRepository _realmTemplates;

    public CharacterService(
        GameDb db,
        CharacterRepository characters,
        CharacterBaseStatRepository baseStats,
        CharacterCurrentStateRepository currentStates,
        RealmTemplateRepository realmTemplates)
    {
        _db = db;
        _characters = characters;
        _baseStats = baseStats;
        _currentStates = currentStates;
        _realmTemplates = realmTemplates;
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
            CreatedAt = DateTime.UtcNow,
        };

        var baseStat = BuildDefaultCharacterBaseStats(character.Id);
        var realmLifespan = await GetRealmLifespanAsync(baseStat.RealmId, cancellationToken);
        var currentState = BuildDefaultCharacterCurrentState(character.Id, baseStat, realmLifespan);

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _characters.CreateAsync(character, cancellationToken);
        await _baseStats.CreateAsync(baseStat, cancellationToken);
        await _currentStates.CreateAsync(currentState, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new CharacterSnapshotDto(
            CharacterDto.FromEntity(character),
            CharacterBaseStatsDto.FromEntity(baseStat, realmLifespan),
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
        var baseStatsDto = row.BaseStats is null ? null : CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan);
        var currentStateDto = row.CurrentState is null ? null : CharacterCurrentStateDto.FromEntity(row.CurrentState);
        return new CharacterSnapshotDto(characterDto, baseStatsDto, currentStateDto);
    }

    public async Task<CharacterSnapshotDto?> LoadCharacterSnapshotAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        // Single roundtrip via join (base stats/current state may not exist yet -> left join).
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
        var baseStatsDto = row.BaseStats is null ? null : CharacterBaseStatsDto.FromEntity(row.BaseStats, realmLifespan);
        var currentStateDto = row.CurrentState is null ? null : CharacterCurrentStateDto.FromEntity(row.CurrentState);
        return new CharacterSnapshotDto(characterDto, baseStatsDto, currentStateDto);
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
            return CharacterBaseStatsDto.FromEntity(existing, existingRealmLifespan);
        }

        var entity = BuildDefaultCharacterBaseStats(characterId);

        await _baseStats.CreateAsync(entity, cancellationToken);
        var realmLifespan = await GetRealmLifespanAsync(entity.RealmId, cancellationToken);
        return CharacterBaseStatsDto.FromEntity(entity, realmLifespan);
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
        existing.BasePhysique = stats.BasePhysique;
        existing.BaseAttack = stats.BaseAttack;
        existing.BaseSpeed = stats.BaseSpeed;
        existing.BaseSpiritualSense = stats.BaseSpiritualSense;
        existing.BaseStamina = stats.BaseStamina;
        existing.LifespanBonus = stats.LifespanBonus;
        existing.BaseFortune = stats.BaseFortune;
        existing.BasePotential = stats.BasePotential;

        await _baseStats.UpdateAsync(existing, cancellationToken);
        var realmLifespan = await GetRealmLifespanAsync(existing.RealmId, cancellationToken);
        return CharacterBaseStatsDto.FromEntity(existing, realmLifespan);
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
        var entity = BuildDefaultCharacterCurrentState(characterId, baseStats, realmLifespan);
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
        existing.RemainingLifespan = state.RemainingLifespan;
        existing.CurrentMapId = state.CurrentMapId;
        existing.CurrentPosX = state.CurrentPosX;
        existing.CurrentPosY = state.CurrentPosY;
        existing.IsDead = state.IsDead;
        existing.CurrentState = state.CurrentState;
        existing.LastSavedAt = NormalizeUtc(state.LastSavedAt);

        await _currentStates.UpdateAsync(existing, cancellationToken);
        return CharacterCurrentStateDto.FromEntity(existing);
    }

    private async Task<bool> IsCharacterNameUniqueInternalAsync(string name, CancellationToken cancellationToken)
    {
        name = NormalizeCharacterName(name);
        return !await _characters.NameExistsAsync(name, cancellationToken);
    }

    private static CharacterCurrentState BuildDefaultCharacterCurrentState(Guid characterId, CharacterBaseStat? baseStat, int? realmLifespan)
    {
        var maxLifespan = CharacterLifespanRules.ResolveMaxLifespan(
            CharacterBaseStatsDto.FromEntity(
                baseStat ?? BuildDefaultCharacterBaseStats(characterId),
                realmLifespan ?? DefaultRealmLifespan),
            DefaultRealmLifespan);

        return new CharacterCurrentState
        {
            CharacterId = characterId,
            CurrentHp = baseStat?.BaseHp ?? DefaultBaseHp,
            CurrentMp = baseStat?.BaseMp ?? DefaultBaseMp,
            CurrentStamina = baseStat?.BaseStamina ?? DefaultBaseStamina,
            RemainingLifespan = CharacterLifespanRules.NormalizeRemainingLifespan(maxLifespan, maxLifespan),
            CurrentMapId = null,
            CurrentPosX = 0,
            CurrentPosY = 0,
            IsDead = false,
            CurrentState = DefaultCurrentStateCode,
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
            BasePhysique = DefaultBasePhysique,
            BaseAttack = DefaultBaseAttack,
            BaseSpeed = DefaultBaseSpeed,
            BaseSpiritualSense = DefaultBaseSpiritualSense,
            BaseStamina = DefaultBaseStamina,
            LifespanBonus = DefaultLifespanBonus,
            BaseFortune = DefaultBaseFortune,
            BasePotential = DefaultBasePotential
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

        // Avoid names that are only spaces/underscores.
        if (!name.Any(char.IsLetterOrDigit))
            throw new GameException(MessageCode.CharacterNameInvalid);

        return name;
    }
}

public sealed record CharacterSnapshotDto(
    CharacterDto Character,
    CharacterBaseStatsDto? BaseStats,
    CharacterCurrentStateDto? CurrentState);
