using GameServer.DTO;
using GameServer.Services;
using GameServer.World;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class CharacterRuntimeSaveService
{
    private readonly WorldManager _worldManager;
    private readonly IServiceScopeFactory _scopeFactory;

    public CharacterRuntimeSaveService(WorldManager worldManager, IServiceScopeFactory scopeFactory)
    {
        _worldManager = worldManager;
        _scopeFactory = scopeFactory;
    }

    public async Task SaveDirtyPlayersAsync(CancellationToken cancellationToken = default)
    {
        foreach (var player in _worldManager.GetOnlinePlayersSnapshot())
        {
            await SavePlayerIfDirtyAsync(player, cancellationToken);
        }
    }

    public async Task FlushPlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        if (!_worldManager.TryGetPlayer(playerId, out var player))
            return;

        await SavePlayerIfDirtyAsync(player, cancellationToken);
    }

    private async Task SavePlayerIfDirtyAsync(PlayerSession player, CancellationToken cancellationToken)
    {
        var snapshot = player.RuntimeState.CaptureSnapshot();
        if (snapshot.DirtyFlags == CharacterRuntimeDirtyFlags.None)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();

        if ((snapshot.DirtyFlags & CharacterRuntimeDirtyFlags.BaseStats) != 0)
        {
            await characterService.UpdateCharacterBaseStatsAsync(snapshot.BaseStats, cancellationToken);
            player.RuntimeState.MarkBaseStatsPersisted(snapshot.BaseStatsVersion);
        }

        if ((snapshot.DirtyFlags & CharacterRuntimeDirtyFlags.CurrentState) != 0)
        {
            var savedAtUtc = DateTime.UtcNow;
            var currentStateToPersist = snapshot.CurrentState with
            {
                CurrentState = snapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Casting
                    ? CharacterRuntimeStateCodes.Idle
                    : snapshot.CurrentState.CurrentState,
                LastSavedAt = savedAtUtc
            };
            await characterService.UpdateCharacterCurrentStateAsync(currentStateToPersist, cancellationToken);
            player.RuntimeState.MarkCurrentStatePersisted(snapshot.CurrentStateVersion, savedAtUtc);
            player.SynchronizeFromCurrentState(currentStateToPersist);
        }
    }
}
