using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class EquipInventoryItemHandler : IPacketHandler<EquipInventoryItemPacket>
{
    private readonly EquipmentService _equipmentService;
    private readonly ItemService _itemService;
    private readonly GameTimeService _gameTimeService;
    private readonly INetworkSender _network;

    public EquipInventoryItemHandler(
        EquipmentService equipmentService,
        ItemService itemService,
        GameTimeService gameTimeService,
        INetworkSender network)
    {
        _equipmentService = equipmentService;
        _itemService = itemService;
        _gameTimeService = gameTimeService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, EquipInventoryItemPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        try
        {
            if (!Enum.IsDefined(typeof(EquipmentSlot), packet.Slot!.Value))
            {
                _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
                {
                    Success = false,
                    Code = MessageCode.EquipmentSlotInvalid
                });
                return;
            }

            var slot = (EquipmentSlot)packet.Slot!.Value;
            await _equipmentService.EquipItemAsync(session.Player.CharacterData.CharacterId, packet.PlayerItemId!.Value, slot);
            var items = await _itemService.GetInventoryAsync(session.Player.CharacterData.CharacterId);
            var snapshot = session.Player.RuntimeState.CaptureSnapshot();

            _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Items = items.Select(x => x.ToModel()).ToList(),
                BaseStats = snapshot.BaseStats.ToModel(),
                CurrentState = snapshot.CurrentState.ToModel(_gameTimeService.GetCurrentSnapshot())
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
