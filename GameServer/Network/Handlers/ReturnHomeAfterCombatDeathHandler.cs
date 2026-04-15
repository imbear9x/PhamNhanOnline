using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Time;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class ReturnHomeAfterCombatDeathHandler : IPacketHandler<ReturnHomeAfterCombatDeathPacket>
{
    private readonly CharacterCombatDeathRecoveryService _deathRecoveryService;
    private readonly INetworkSender _server;
    private readonly GameTimeService _gameTimeService;

    public ReturnHomeAfterCombatDeathHandler(
        CharacterCombatDeathRecoveryService deathRecoveryService,
        INetworkSender server,
        GameTimeService gameTimeService)
    {
        _deathRecoveryService = deathRecoveryService;
        _server = server;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, ReturnHomeAfterCombatDeathPacket packet)
    {
        if (session.Player == null)
        {
            SendFailure(session, MessageCode.CharacterNotFound);
            return;
        }

        var player = session.Player;
        if (!_deathRecoveryService.IsCombatDead(player.RuntimeState.CaptureSnapshot().CurrentState))
        {
            SendFailure(session, MessageCode.CharacterNotCombatDead);
            return;
        }

        var runtimeSnapshot = await _deathRecoveryService.RecoverOnlinePlayerToHomeAsync(player);
        if (runtimeSnapshot is null)
        {
            SendFailure(session, MessageCode.CharacterNotCombatDead);
            return;
        }

        session.AreCharacterActionsRestricted = false;
        _server.Send(session.ConnectionId, new ReturnHomeAfterCombatDeathResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            BaseStats = runtimeSnapshot.BaseStats.ToModel(),
            CurrentState = runtimeSnapshot.CurrentState.ToModel(player.CharacterData, runtimeSnapshot.BaseStats, _gameTimeService.GetCurrentSnapshot())
        });
    }

    private void SendFailure(ConnectionSession session, MessageCode code)
    {
        _server.Send(session.ConnectionId, new ReturnHomeAfterCombatDeathResultPacket
        {
            Success = false,
            Code = code
        });
    }
}
