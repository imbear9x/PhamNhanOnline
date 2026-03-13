using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameShared.Messages;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Services;

public sealed class CharacterService
{
    private const int DefaultCurrentStateCode = 0;

    private readonly GameDb _db;
    private readonly CharacterRepository _characters;
    private readonly CharacterBaseStatRepository _baseStats;
    private readonly CharacterCurrentStateRepository _currentStates;

    public CharacterService(
        GameDb db,
        CharacterRepository characters,
        CharacterBaseStatRepository baseStats,
        CharacterCurrentStateRepository currentStates)
    {
        _db = db;
        _characters = characters;
        _baseStats = baseStats;
        _currentStates = currentStates;
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

        var baseStat = new CharacterBaseStat
        {
            CharacterId = character.Id,
            RealmId = 1,
            Cultivation = 0,
        };
        var currentState = BuildDefaultCharacterCurrentState(character.Id, baseStat);

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _characters.CreateAsync(character, cancellationToken);
        await _baseStats.CreateAsync(baseStat, cancellationToken);
        await _currentStates.CreateAsync(currentState, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new CharacterSnapshotDto(
            CharacterDto.FromEntity(character),
            CharacterBaseStatsDto.FromEntity(baseStat),
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

        var characterDto = CharacterDto.FromEntity(row.Character);
        var baseStatsDto = row.BaseStats is null ? null : CharacterBaseStatsDto.FromEntity(row.BaseStats);
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

        var characterDto = CharacterDto.FromEntity(row.Character);
        var baseStatsDto = row.BaseStats is null ? null : CharacterBaseStatsDto.FromEntity(row.BaseStats);
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
            return CharacterBaseStatsDto.FromEntity(existing);

        var entity = new CharacterBaseStat
        {
            CharacterId = characterId,
            RealmId = 1,
            Cultivation = 0,
        };

        await _baseStats.CreateAsync(entity, cancellationToken);
        return CharacterBaseStatsDto.FromEntity(entity);
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
        var entity = BuildDefaultCharacterCurrentState(characterId, baseStats);
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

    private static CharacterCurrentState BuildDefaultCharacterCurrentState(Guid characterId, CharacterBaseStat? baseStat)
    {
        return new CharacterCurrentState
        {
            CharacterId = characterId,
            CurrentHp = baseStat?.BaseHp ?? 100,
            CurrentMp = baseStat?.BaseMp ?? 100,
            CurrentMapId = null,
            CurrentPosX = 0,
            CurrentPosY = 0,
            IsDead = false,
            CurrentState = DefaultCurrentStateCode,
            LastSavedAt = DateTime.UtcNow
        };
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
