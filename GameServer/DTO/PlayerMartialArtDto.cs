namespace GameServer.DTO;

public sealed record PlayerMartialArtDto(
    int MartialArtId,
    string Code,
    string Name,
    string? Icon,
    int Quality,
    string? Category,
    string? Description,
    int CurrentStage,
    long CurrentExp,
    long ExpRequired,
    int MaxStage,
    decimal QiAbsorptionRate,
    bool IsActive);
