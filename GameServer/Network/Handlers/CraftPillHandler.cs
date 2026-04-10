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
    private readonly AlchemyPracticeService _alchemyPracticeService;
    private readonly INetworkSender _network;

    public CraftPillHandler(
        AlchemyPracticeService alchemyPracticeService,
        INetworkSender network)
    {
        _alchemyPracticeService = alchemyPracticeService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, CraftPillPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new CraftPillResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                PillRecipeTemplateId = packet.PillRecipeTemplateId
            });
            return;
        }

        var result = await _alchemyPracticeService.StartCraftAsync(
            session,
            packet.PillRecipeTemplateId!.Value,
            packet.SelectedPlayerItemIds,
            packet.SelectedOptionalInputIds);

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
