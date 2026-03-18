namespace GameServer.DTO;

public sealed record PlayerMartialArtDto(
    int MartialArtId,
    string Code,
    string Name,
    int Quality,
    string? Category,
    int CurrentStage,
    long CurrentExp,
    int MaxStage,
    decimal QiAbsorptionRate,
    bool IsActive);
