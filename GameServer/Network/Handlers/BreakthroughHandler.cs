using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Time;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class BreakthroughHandler : IPacketHandler<BreakthroughPacket>
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public BreakthroughHandler(
        CharacterCultivationService cultivationService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationService = cultivationService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, BreakthroughPacket packet)
    {
        var result = await _cultivationService.BreakthroughAsync(session);
        _network.Send(session.ConnectionId, new BreakthroughResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            BaseStats = result.BaseStats?.ToModel(),
            CurrentState = result.CurrentState?.ToModel(_gameTimeService.GetCurrentSnapshot())
        });
    }
}
