using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Time;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class StartCultivationHandler : IPacketHandler<StartCultivationPacket>
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public StartCultivationHandler(
        CharacterCultivationService cultivationService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationService = cultivationService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, StartCultivationPacket packet)
    {
        var result = await _cultivationService.StartCultivationAsync(session);
        var baseStats = session.Player?.RuntimeState.CaptureSnapshot().BaseStats;
        _network.Send(session.ConnectionId, new StartCultivationResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            CurrentState = result.CurrentState is null || session.Player is null
                ? null
                : result.CurrentState.ToModel(session.Player.CharacterData, baseStats, _gameTimeService.GetCurrentSnapshot())
        });
    }
}
