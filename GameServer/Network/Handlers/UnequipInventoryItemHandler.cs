using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class UnequipInventoryItemHandler : IPacketHandler<UnequipInventoryItemPacket>
{
    private readonly EquipmentService _equipmentService;
    private readonly ItemService _itemService;
    private readonly GameTimeService _gameTimeService;
    private readonly INetworkSender _network;

    public UnequipInventoryItemHandler(
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

    public async Task HandleAsync(ConnectionSession session, UnequipInventoryItemPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        if (!Enum.IsDefined(typeof(EquipmentSlot), packet.Slot!.Value))
        {
            _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.EquipmentSlotInvalid
            });
            return;
        }

        var slot = (EquipmentSlot)packet.Slot!.Value;
        var changed = await _equipmentService.UnequipItemAsync(session.Player.CharacterData.CharacterId, slot);
        if (!changed)
        {
            _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.EquipmentSlotEmpty
            });
            return;
        }

        var items = await _itemService.GetInventoryAsync(session.Player.CharacterData.CharacterId);
        var snapshot = session.Player.RuntimeState.CaptureSnapshot();

        _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            Items = items.Select(x => x.ToModel()).ToList(),
            BaseStats = snapshot.BaseStats.ToModel(),
            CurrentState = snapshot.CurrentState.ToModel(_gameTimeService.GetCurrentSnapshot())
        });
    }
}
