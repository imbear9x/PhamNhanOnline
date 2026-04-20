using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameServer.Time;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class CreateCharacterHandler : IPacketHandler<CreateCharacterPacket>
{
    private readonly CharacterCreationActionService _characterCreationActionService;
    private readonly INetworkSender _server;
    private readonly GameTimeService _gameTimeService;

    public CreateCharacterHandler(
        CharacterCreationActionService characterCreationActionService,
        INetworkSender server,
        GameTimeService gameTimeService)
    {
        _characterCreationActionService = characterCreationActionService;
        _server = server;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, CreateCharacterPacket packet)
    {
        try
        {
            var result = await _characterCreationActionService.CreateAsync(
                session.PlayerId,
                packet.Name!,
                packet.ServerId!.Value,
                packet.ModelId!.Value);
            if (!result.Success || result.Snapshot is null)
            {
                _server.Send(session.ConnectionId, new CreateCharacterResultPacket
                {
                    Success = false,
                    Code = result.Code
                });
                return;
            }

            var created = result.Snapshot;

            _server.Send(session.ConnectionId, new CreateCharacterResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Character = created.Character.ToModel(),
                BaseStats = created.BaseStats?.ToModel(),
                CurrentState = created.CurrentState is null
                    ? null
                    : created.CurrentState.ToModel(created.Character, created.BaseStats, _gameTimeService.GetCurrentSnapshot())
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
