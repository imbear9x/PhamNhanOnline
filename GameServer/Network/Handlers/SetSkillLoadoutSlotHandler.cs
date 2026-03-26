using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class SetSkillLoadoutSlotHandler : IPacketHandler<SetSkillLoadoutSlotPacket>
{
    private readonly SkillService _skillService;
    private readonly INetworkSender _network;

    public SetSkillLoadoutSlotHandler(
        SkillService skillService,
        INetworkSender network)
    {
        _skillService = skillService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, SetSkillLoadoutSlotPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new SetSkillLoadoutSlotResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        try
        {
            var snapshot = await _skillService.SetSkillLoadoutSlotAsync(
                session.Player.CharacterData.CharacterId,
                packet.SlotIndex!.Value,
                packet.PlayerSkillId);

            _network.Send(session.ConnectionId, new SetSkillLoadoutSlotResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                MaxLoadoutSlotCount = snapshot.MaxLoadoutSlotCount,
                Skills = snapshot.Skills.Select(static x => x.ToModel()).ToList(),
                LoadoutSlots = snapshot.LoadoutSlots.Select(static x => x.ToModel()).ToList()
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new SetSkillLoadoutSlotResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
