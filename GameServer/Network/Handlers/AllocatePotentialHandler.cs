using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class AllocatePotentialHandler : IPacketHandler<AllocatePotentialPacket>
{
    private readonly CultivationActionService _cultivationActionService;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public AllocatePotentialHandler(
        CultivationActionService cultivationActionService,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _cultivationActionService = cultivationActionService;
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, AllocatePotentialPacket packet)
    {
        var target = Enum.IsDefined(typeof(PotentialAllocationTarget), packet.TargetStat ?? 0)
            ? (PotentialAllocationTarget)(packet.TargetStat ?? 0)
            : PotentialAllocationTarget.None;
        var execution = await _cultivationActionService.AllocatePotentialAsync(
            session,
            target,
            packet.RequestedPotentialAmount ?? 0);

        _network.Send(session.ConnectionId, new AllocatePotentialResultPacket
        {
            Success = execution.Result.Success,
            Code = execution.Result.Code,
            BaseStats = execution.BaseStats?.ToModel(),
            CurrentState = execution.CurrentState is null || execution.BaseStats is null || session.Player is null
                ? null
                : execution.CurrentState.ToModel(session.Player.CharacterData, execution.BaseStats, _gameTimeService.GetCurrentSnapshot()),
            RequestedPotentialAmount = execution.Result.PotentialAllocation?.RequestedPotentialAmount,
            SpentPotentialAmount = execution.Result.PotentialAllocation?.SpentPotentialAmount,
            AppliedUpgradeCount = execution.Result.PotentialAllocation?.AppliedUpgradeCount
        });
    }
}
