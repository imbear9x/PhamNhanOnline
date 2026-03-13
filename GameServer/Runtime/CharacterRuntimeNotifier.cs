using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.World;
using GameShared.Packets;

namespace GameServer.Runtime;

public sealed class CharacterRuntimeNotifier
{
    private readonly INetworkSender _network;

    public CharacterRuntimeNotifier(INetworkSender network)
    {
        _network = network;
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
        _network.Send(player.ConnectionId, new CharacterCurrentStateChangedPacket
        {
            CurrentState = currentState.ToModel()
        });
    }
}
