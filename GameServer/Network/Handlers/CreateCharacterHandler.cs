using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class CreateCharacterHandler : IPacketHandler<CreateCharacterPacket>
{
    private readonly CharacterService _characterService;
    private readonly INetworkSender _server;

    public CreateCharacterHandler(CharacterService characterService, INetworkSender server)
    {
        _characterService = characterService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, CreateCharacterPacket packet)
    {
        try
        {
            var created = await _characterService.CreateCharacterAsync(
                session.PlayerId,
                packet.Name!,
                packet.ServerId!.Value,
                packet.ModelId!.Value);

            _server.Send(session.ConnectionId, new CreateCharacterResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Character = created.Character.ToModel(),
                BaseStats = created.BaseStats?.ToModel(),
                CurrentState = created.CurrentState?.ToModel()
            });
        }
        catch (GameException ex)
        {
            _server.Send(session.ConnectionId, new CreateCharacterResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
        catch (Exception)
        {
            _server.Send(session.ConnectionId, new CreateCharacterResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError
            });
            throw;
        }
    }
}
