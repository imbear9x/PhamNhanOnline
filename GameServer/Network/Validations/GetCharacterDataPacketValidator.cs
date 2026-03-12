using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class GetCharacterDataPacketValidator : PacketValidator<GetCharacterDataPacket>
{
    public override bool TryValidate(GetCharacterDataPacket packet, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(packet, out var code))
        {
            errorPacket = new GetCharacterDataResultPacket
            {
                Success = false,
                Code = code
            };
            return false;
        }

        if (packet.CharacterId == Guid.Empty)
        {
            errorPacket = new GetCharacterDataResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterIdInvalid
            };
            return false;
        }

        errorPacket = null;
        return true;
    }
}
