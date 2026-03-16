using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Time;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class StopCultivationHandler : IPacketHandler<StopCultivationPacket>
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public StopCultivationHandler(
        CharacterCultivationService cultivationService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationService = cultivationService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, StopCultivationPacket packet)
    {
        var result = await _cultivationService.StopCultivationAsync(session);
        _network.Send(session.ConnectionId, new StopCultivationResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            CurrentState = result.CurrentState?.ToModel(_gameTimeService.GetCurrentSnapshot())
        });
    }
}
