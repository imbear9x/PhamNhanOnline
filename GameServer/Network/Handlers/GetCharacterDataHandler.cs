using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetCharacterDataHandler : IPacketHandler<GetCharacterDataPacket>
{
    private readonly CharacterService _characterService;
    private readonly INetworkSender _server;

    public GetCharacterDataHandler(CharacterService characterService, INetworkSender server)
    {
        _characterService = characterService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, GetCharacterDataPacket packet)
    {
        try
        {
            var data = await _characterService.LoadCharacterSnapshotByAccountAsync(
                session.PlayerId,
                packet.CharacterId!.Value);

            if (data is null)
            {
                _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
                {
                    Success = false,
                    Code = MessageCode.CharacterNotFound
                });
                return;
            }

            session.SelectedCharacterId = data.Character.CharacterId;

            _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Character = data.Character.ToModel(),
                BaseStats = data.BaseStats?.ToModel(),
                CurrentState = data.CurrentState?.ToModel()
            });
        }
        catch (Exception)
        {
            _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError
            });
            throw;
        }
    }
}
