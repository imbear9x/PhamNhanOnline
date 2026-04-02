using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Time;
using GameServer.World;
using GameShared.Packets;

namespace GameServer.Runtime;

public sealed class CharacterRuntimeNotifier
{
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public CharacterRuntimeNotifier(INetworkSender network, GameTimeService gameTimeService)
    {
        _network = network;
        _gameTimeService = gameTimeService;
    }

    public void NotifyBaseStatsChanged(PlayerSession player, CharacterBaseStatsDto baseStats)
    {
        _network.Send(player.ConnectionId, new CharacterBaseStatsChangedPacket
        {
            BaseStats = baseStats.ToModel()
        });
    }

    public void NotifyCurrentStateChanged(PlayerSession player, CharacterCurrentStateDto currentState)
    {
        var gameTime = _gameTimeService.GetCurrentSnapshot();
        var remainingLifespan = CharacterLifespanRules.CalculateRemainingLifespanYears(currentState.LifespanEndGameMinute, gameTime);
        player.TryUpdateReportedRemainingLifespan(remainingLifespan);

        _network.Send(player.ConnectionId, new CharacterCurrentStateChangedPacket
        {
            CurrentState = currentState.ToModel(gameTime)
        });
    }

    public bool TryNotifyTimeDerivedCurrentStateChanged(PlayerSession player, CharacterCurrentStateDto currentState)
    {
        var gameTime = _gameTimeService.GetCurrentSnapshot();
        var remainingLifespan = CharacterLifespanRules.CalculateRemainingLifespanYears(currentState.LifespanEndGameMinute, gameTime);
        if (!player.TryUpdateReportedRemainingLifespan(remainingLifespan))
            return false;

        _network.Send(player.ConnectionId, new CharacterCurrentStateChangedPacket
        {
            CurrentState = currentState.ToModel(gameTime)
        });

        return true;
    }

    public void NotifyStateTransition(PlayerSession player, int reason)
    {
        _network.Send(player.ConnectionId, new CharacterStateTransitionPacket
        {
            CharacterId = player.CharacterData.CharacterId,
            Reason = reason
        });
    }
}
