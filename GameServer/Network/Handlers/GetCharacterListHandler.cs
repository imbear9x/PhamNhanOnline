using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetCharacterListHandler : IPacketHandler<GetCharacterListPacket>
{
    private readonly CharacterService _characterService;
    private readonly INetworkSender _server;

    public GetCharacterListHandler(CharacterService characterService, INetworkSender server)
    {
        _characterService = characterService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, GetCharacterListPacket packet)
    {
        try
        {
            var characters = await _characterService.GetCharactersByAccountAsync(session.PlayerId);

            _server.Send(session.ConnectionId, new GetCharacterListResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Characters = characters.Select(static x => x.ToModel()).ToList()
            });
        }
        catch (Exception)
        {
            _server.Send(session.ConnectionId, new GetCharacterListResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError,
                Characters = new List<GameShared.Models.CharacterModel>()
            });
            throw;
        }
    }
}
