using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class MartialArtActionService
{
    private readonly MartialArtService _martialArtService;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly CharacterCultivationService _cultivationService;

    public MartialArtActionService(
        MartialArtService martialArtService,
        CharacterFinalStatService characterFinalStatService,
        CharacterCultivationService cultivationService)
    {
        _martialArtService = martialArtService;
        _characterFinalStatService = characterFinalStatService;
        _cultivationService = cultivationService;
    }

    public async Task<SetActiveMartialArtExecutionResult> SetActiveMartialArtAsync(
        ConnectionSession session,
        int requestedMartialArtId,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            throw new GameException(MessageCode.CharacterMustEnterWorld);

        var currentSnapshot = session.Player.RuntimeState.CaptureSnapshot();
        if (currentSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Cultivating ||
            currentSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Practicing)
        {
            throw new GameException(MessageCode.PracticeAlreadyActive);
        }

        var updatedBaseStats = await _martialArtService.SetActiveMartialArtAsync(
            session.Player.CharacterData.CharacterId,
            requestedMartialArtId,
            cancellationToken);

        session.Player.RuntimeState.UpdateBaseStats(_ => updatedBaseStats);
        var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(session.Player, cancellationToken);
        var cultivationPreview = await _cultivationService.BuildCultivationPreviewAsync(runtimeSnapshot.BaseStats, cancellationToken);

        return new SetActiveMartialArtExecutionResult(runtimeSnapshot.BaseStats, cultivationPreview);
    }
}

public readonly record struct SetActiveMartialArtExecutionResult(
    CharacterBaseStatsDto BaseStats,
    CultivationPreviewDto? CultivationPreview);
