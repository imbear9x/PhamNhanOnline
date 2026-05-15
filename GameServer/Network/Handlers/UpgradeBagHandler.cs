using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class UpgradeBagHandler : IPacketHandler<UpgradeBagPacket>
{
    private readonly BagService _bagService;
    private readonly INetworkSender _network;

    public UpgradeBagHandler(BagService bagService, INetworkSender network)
    {
        _bagService = bagService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, UpgradeBagPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new UpgradeBagResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var result = await _bagService.UpgradeBagAsync(session.Player.CharacterData.CharacterId, packet.TargetGrade ?? 0);
        _network.Send(session.ConnectionId, new UpgradeBagResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            RemainingLinhThach = result.RemainingLinhThach,
            FailureReason = result.FailureReason,
            BagState = result.BagState is null
                ? null
                : new BagStateModel
                {
                    Grade = result.BagState.Grade,
                    UsedSlots = result.BagState.UsedSlots,
                    TotalSlots = result.BagState.TotalSlots,
                    DisplayName = result.BagState.DisplayName
                }
        });
    }
}