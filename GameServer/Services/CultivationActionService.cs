using GameServer.DTO;
using GameServer.Network;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Models;

namespace GameServer.Services;

public sealed class CultivationActionService
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly CharacterFinalStatService _characterFinalStatService;

    public CultivationActionService(
        CharacterCultivationService cultivationService,
        CharacterFinalStatService characterFinalStatService)
    {
        _cultivationService = cultivationService;
        _characterFinalStatService = characterFinalStatService;
    }

    public async Task<CultivationActionExecutionResult> StartCultivationAsync(
        ConnectionSession session,
        CancellationToken cancellationToken = default)
    {
        var result = await _cultivationService.StartCultivationAsync(session, cancellationToken);
        return new CultivationActionExecutionResult(result, result.BaseStats, result.CurrentState);
    }

    public async Task<CultivationActionExecutionResult> StopCultivationAsync(
        ConnectionSession session,
        CancellationToken cancellationToken = default)
    {
        var result = await _cultivationService.StopCultivationAsync(session, cancellationToken);
        return new CultivationActionExecutionResult(result, result.BaseStats, result.CurrentState);
    }

    public async Task<CultivationActionExecutionResult> BreakthroughAsync(
        ConnectionSession session,
        CancellationToken cancellationToken = default)
    {
        var result = await _cultivationService.BreakthroughAsync(session, cancellationToken);
        return await BuildFinalStatAwareResultAsync(session, result, cancellationToken);
    }

    public async Task<CultivationActionExecutionResult> AllocatePotentialAsync(
        ConnectionSession session,
        PotentialAllocationTarget target,
        int requestedPotentialAmount,
        CancellationToken cancellationToken = default)
    {
        var result = await _cultivationService.AllocatePotentialAsync(
            session,
            target,
            requestedPotentialAmount,
            cancellationToken);
        return await BuildFinalStatAwareResultAsync(session, result, cancellationToken);
    }

    private async Task<CultivationActionExecutionResult> BuildFinalStatAwareResultAsync(
        ConnectionSession session,
        CharacterCultivationService.CultivationActionResult result,
        CancellationToken cancellationToken)
    {
        CharacterBaseStatsDto? responseBaseStats = result.BaseStats;
        CharacterCurrentStateDto? responseCurrentState = result.CurrentState;

        if (result.Success && session.Player is not null)
        {
            var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(session.Player, cancellationToken);
            responseBaseStats = runtimeSnapshot.BaseStats;
            responseCurrentState = runtimeSnapshot.CurrentState;
        }

        return new CultivationActionExecutionResult(result, responseBaseStats, responseCurrentState);
    }
}

public readonly record struct CultivationActionExecutionResult(
    CharacterCultivationService.CultivationActionResult Result,
    CharacterBaseStatsDto? BaseStats,
    CharacterCurrentStateDto? CurrentState);
