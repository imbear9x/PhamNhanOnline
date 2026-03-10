using GameServer.DTO;
using GameServer.Entities;
using GameServer.Repositories;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Services;

public sealed class CharacterService
{
    private readonly GameDb _db;
    private readonly CharacterRepository _characters;
    private readonly CharacterStatRepository _stats;

    public CharacterService(GameDb db, CharacterRepository characters, CharacterStatRepository stats)
    {
        _db = db;
        _characters = characters;
        _stats = stats;
    }

    public async Task<List<CharacterDto>> GetCharactersByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var entities = await _characters.ListByAccountAsync(accountId, cancellationToken);
        return entities.Select(CharacterDto.FromEntity).ToList();
    }

    public Task<bool> IsCharacterNameUniqueAsync(string name, CancellationToken cancellationToken = default) =>
        IsCharacterNameUniqueInternalAsync(name, cancellationToken);

    public async Task<CharacterWithStatsDto> CreateCharacterAsync(
        Guid accountId,
        string name,
        int serverId,
        int modelId,
        CancellationToken cancellationToken = default)
    {
        name = NormalizeCharacterName(name);

        var existing = await _characters.ListByAccountAsync(accountId, cancellationToken);
        if (existing.Count != 0)
            throw new InvalidOperationException("Account already has a character.");

        if (!await IsCharacterNameUniqueInternalAsync(name, cancellationToken))
            throw new InvalidOperationException("Character name already exists.");

        var character = new Character
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ServerId = serverId,
            Name = name,
            ModelId = modelId,
            CreatedAt = DateTime.UtcNow,
        };

        var stat = new CharacterStat
        {
            CharacterId = character.Id,
            RealmId = 1,
            Cultivation = 0,
        };

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _characters.CreateAsync(character, cancellationToken);
        await _stats.CreateAsync(stat, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new CharacterWithStatsDto(CharacterDto.FromEntity(character), CharacterStatsDto.FromEntity(stat));
    }

    public async Task<CharacterWithStatsDto?> LoadCharacterWithStatsAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        // Single roundtrip via join (stats may not exist yet -> left join).
        var query =
            from c in _db.GetTable<Character>()
            where c.Id == characterId
            from s in _db.GetTable<CharacterStat>().LeftJoin(st => st.CharacterId == c.Id)
            select new { Character = c, Stats = s };

        var row = await query.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var characterDto = CharacterDto.FromEntity(row.Character);
        var statsDto = row.Stats is null ? null : CharacterStatsDto.FromEntity(row.Stats);
        return new CharacterWithStatsDto(characterDto, statsDto);
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
            throw new InvalidOperationException("Character not found.");

        if (!string.Equals(entity.Name, name, StringComparison.Ordinal))
        {
            if (!await IsCharacterNameUniqueInternalAsync(name, cancellationToken))
                throw new InvalidOperationException("Character name already exists.");
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

    public async Task<CharacterStatsDto> InitializeCharacterStatsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var character = await _characters.GetByIdAsync(characterId, cancellationToken);
        if (character is null)
            throw new InvalidOperationException("Character not found.");

        var existing = await _stats.GetByIdAsync(characterId, cancellationToken);
        if (existing is not null)
            return CharacterStatsDto.FromEntity(existing);

        var entity = new CharacterStat
        {
            CharacterId = characterId,
            RealmId = 1,
            Cultivation = 0,
        };

        await _stats.CreateAsync(entity, cancellationToken);
        return CharacterStatsDto.FromEntity(entity);
    }

    private async Task<bool> IsCharacterNameUniqueInternalAsync(string name, CancellationToken cancellationToken)
    {
        name = NormalizeCharacterName(name);
        return !await _characters.NameExistsAsync(name, cancellationToken);
    }

    private static string NormalizeCharacterName(string name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length is < 3 or > 20)
            throw new ArgumentException("Character name must be 3-20 characters.", nameof(name));

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            var ok = char.IsLetterOrDigit(ch) || ch == ' ' || ch == '_';
            if (!ok)
                throw new ArgumentException("Character name cannot contain special characters.", nameof(name));
        }

        // Avoid names that are only spaces/underscores.
        if (!name.Any(char.IsLetterOrDigit))
            throw new ArgumentException("Character name must contain letters or digits.", nameof(name));

        return name;
    }
}

public sealed record CharacterWithStatsDto(CharacterDto Character, CharacterStatsDto? Stats);

