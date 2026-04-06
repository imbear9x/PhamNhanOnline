using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class CharacterLifecycleService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;
    private readonly WorldInterestService _interestService;

    public CharacterLifecycleService(
        IServiceScopeFactory scopeFactory,
        CharacterRuntimeSaveService runtimeSaveService,
        INetworkSender network,
        GameTimeService gameTimeService,
        WorldInterestService interestService)
    {
        _scopeFactory = scopeFactory;
        _runtimeSaveService = runtimeSaveService;
        _network = network;
        _gameTimeService = gameTimeService;
        _interestService = interestService;
    }

    public bool IsLifespanExpired(CharacterCurrentStateDto? currentState)
    {
        if (currentState is null)
            return false;

        return CharacterLifespanRules.CalculateRemainingLifespanYears(
                   currentState.LifespanEndGameMinute,
                   _gameTimeService.GetCurrentSnapshot()) <= 0;
    }

    public async Task<CharacterSnapshotDto> PrepareSnapshotForWorldEntryAsync(
        CharacterSnapshotDto snapshot,
        CancellationToken cancellationToken = default)
    {
        if (!IsLifespanExpired(snapshot.CurrentState) || snapshot.CurrentState is null)
            return snapshot;

        var updatedState = await PersistLifespanExpiredStateIfNeededAsync(snapshot.CurrentState, cancellationToken);
        return snapshot with { CurrentState = updatedState };
    }

    public async Task HandleLifespanExpiredAsync(PlayerSession player, CancellationToken cancellationToken = default)
    {
        var snapshot = player.RuntimeState.UpdateCurrentState(MarkLifespanExpired);
        player.SetCharacterActionsRestricted(true);
        player.SynchronizeFromCurrentState(snapshot.CurrentState);

        if (player.IsLifespanExpiredProcessed)
            return;

        player.MarkLifespanExpiredProcessed();
        _network.Send(player.ConnectionId, new CharacterCurrentStateChangedPacket
        {
            CurrentState = snapshot.CurrentState.ToModel(_gameTimeService.GetCurrentSnapshot())
        });
        _interestService.NotifyCurrentStateChanged(player, snapshot.CurrentState);
        _network.Send(player.ConnectionId, new CharacterStateTransitionPacket
        {
            CharacterId = player.CharacterData.CharacterId,
            Reason = CharacterStateTransitionReasons.LifespanExpired
        });

        await _runtimeSaveService.FlushPlayerAsync(player.PlayerId, cancellationToken);
    }

    public void NotifyLifespanExpired(int connectionId, Guid characterId)
    {
        _network.Send(connectionId, new CharacterStateTransitionPacket
        {
            CharacterId = characterId,
            Reason = CharacterStateTransitionReasons.LifespanExpired
        });
    }

    private async Task<CharacterCurrentStateDto> PersistLifespanExpiredStateIfNeededAsync(
        CharacterCurrentStateDto currentState,
        CancellationToken cancellationToken)
    {
        var expiredState = MarkLifespanExpired(currentState);
        if (expiredState == currentState)
            return currentState;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();
        return await characterService.UpdateCharacterCurrentStateAsync(expiredState, cancellationToken);
    }

    private static CharacterCurrentStateDto MarkLifespanExpired(CharacterCurrentStateDto currentState)
    {
        if (currentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired &&
            currentState.IsExpired &&
            currentState.CurrentHp == 0)
        {
            return currentState;
        }

        return currentState with
        {
            CurrentHp = 0,
            IsExpired = true,
            CurrentState = CharacterRuntimeStateCodes.LifespanExpired,
            LastSavedAt = DateTime.UtcNow
        };
    }
}
