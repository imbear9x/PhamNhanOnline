using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Time;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class AllocatePotentialHandler : IPacketHandler<AllocatePotentialPacket>
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public AllocatePotentialHandler(
        CharacterCultivationService cultivationService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationService = cultivationService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, AllocatePotentialPacket packet)
    {
        var target = Enum.IsDefined(typeof(PotentialAllocationTarget), packet.TargetStat ?? 0)
            ? (PotentialAllocationTarget)(packet.TargetStat ?? 0)
            : PotentialAllocationTarget.None;
        var result = await _cultivationService.AllocatePotentialAsync(
            session,
            target,
            packet.Amount ?? 0);

        _network.Send(session.ConnectionId, new AllocatePotentialResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            BaseStats = result.BaseStats?.ToModel(),
            CurrentState = result.CurrentState?.ToModel(_gameTimeService.GetCurrentSnapshot())
        });
    }
}
