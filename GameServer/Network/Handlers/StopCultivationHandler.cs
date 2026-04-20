using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class StopCultivationHandler : IPacketHandler<StopCultivationPacket>
{
    private readonly CultivationActionService _cultivationActionService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public StopCultivationHandler(
        CultivationActionService cultivationActionService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationActionService = cultivationActionService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, StopCultivationPacket packet)
    {
        var execution = await _cultivationActionService.StopCultivationAsync(session);
        _network.Send(session.ConnectionId, new StopCultivationResultPacket
        {
            Success = execution.Result.Success,
            Code = execution.Result.Code,
            CurrentState = execution.CurrentState is null || session.Player is null
                ? null
                : execution.CurrentState.ToModel(session.Player.CharacterData, execution.BaseStats, _gameTimeService.GetCurrentSnapshot())
        });
    }
}
