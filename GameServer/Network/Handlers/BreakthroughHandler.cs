using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class BreakthroughHandler : IPacketHandler<BreakthroughPacket>
{
    private readonly CultivationActionService _cultivationActionService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public BreakthroughHandler(
        CultivationActionService cultivationActionService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationActionService = cultivationActionService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, BreakthroughPacket packet)
    {
        var execution = await _cultivationActionService.BreakthroughAsync(session);

        _network.Send(session.ConnectionId, new BreakthroughResultPacket
        {
            Success = execution.Result.Success,
            Code = execution.Result.Code,
            BaseStats = execution.BaseStats?.ToModel(),
            CurrentState = execution.CurrentState is null || execution.BaseStats is null || session.Player is null
                ? null
                : execution.CurrentState.ToModel(session.Player.CharacterData, execution.BaseStats, _gameTimeService.GetCurrentSnapshot())
        });
    }
}
