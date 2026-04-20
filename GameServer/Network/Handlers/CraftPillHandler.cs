using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class CraftPillHandler : IPacketHandler<CraftPillPacket>
{
    private readonly AlchemyCraftActionService _alchemyCraftActionService;
    private readonly INetworkSender _network;

    public CraftPillHandler(
        AlchemyCraftActionService alchemyCraftActionService,
        INetworkSender network)
    {
        _alchemyCraftActionService = alchemyCraftActionService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, CraftPillPacket packet)
    {
        var result = await _alchemyCraftActionService.StartCraftAsync(
            session,
            packet.PillRecipeTemplateId!.Value,
            packet.RequestedCraftCount!.Value,
            packet.SelectedPlayerItemIds,
            packet.SelectedOptionalInputs);

        _network.Send(session.ConnectionId, new CraftPillResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            FailureReason = result.FailureReason,
            PillRecipeTemplateId = packet.PillRecipeTemplateId,
            Session = result.Session,
            ConsumedItems = result.ConsumedItems.Count > 0 ? result.ConsumedItems.ToList() : null,
            Items = result.InventoryItems.Count > 0 ? result.InventoryItems.ToList() : null,
            Recipe = result.Recipe
        });
    }
}
