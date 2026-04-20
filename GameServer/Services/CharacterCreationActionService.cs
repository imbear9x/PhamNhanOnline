using GameServer.DTO;
using GameServer.Exceptions;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class CharacterCreationActionService
{
    private readonly CharacterService _characterService;

    public CharacterCreationActionService(CharacterService characterService)
    {
        _characterService = characterService;
    }

    public async Task<CharacterCreationActionResult> CreateAsync(
        Guid accountId,
        string name,
        int serverId,
        int modelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _characterService.CreateCharacterAsync(
                accountId,
                name,
                serverId,
                modelId,
                cancellationToken);
            return CharacterCreationActionResult.SuccessResult(snapshot);
        }
        catch (GameException ex)
        {
            return CharacterCreationActionResult.Failure(ex.Code);
        }
    }
}

public readonly record struct CharacterCreationActionResult(
    bool Success,
    MessageCode Code,
    CharacterSnapshotDto? Snapshot)
{
    public static CharacterCreationActionResult SuccessResult(CharacterSnapshotDto snapshot) =>
        new(true, MessageCode.None, snapshot);

    public static CharacterCreationActionResult Failure(MessageCode code) =>
        new(false, code, null);
}
