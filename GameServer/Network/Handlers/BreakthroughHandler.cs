using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class BreakthroughHandler : IPacketHandler<BreakthroughPacket>
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public BreakthroughHandler(
        CharacterCultivationService cultivationService,
        CharacterFinalStatService characterFinalStatService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationService = cultivationService;
        _characterFinalStatService = characterFinalStatService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, BreakthroughPacket packet)
    {
        var result = await _cultivationService.BreakthroughAsync(session);
        CharacterBaseStatsDto? responseBaseStats = result.BaseStats;
        CharacterCurrentStateDto? responseCurrentState = result.CurrentState;
        if (result.Success && session.Player is not null)
        {
            var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(session.Player);
            responseBaseStats = runtimeSnapshot.BaseStats;
            responseCurrentState = runtimeSnapshot.CurrentState;
        }

        _network.Send(session.ConnectionId, new BreakthroughResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            BaseStats = responseBaseStats?.ToModel(),
            CurrentState = responseCurrentState is null || responseBaseStats is null || session.Player is null
                ? null
                : responseCurrentState.ToModel(session.Player.CharacterData, responseBaseStats, _gameTimeService.GetCurrentSnapshot())
        });
    }
}
