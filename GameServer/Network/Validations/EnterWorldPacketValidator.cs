using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class EnterWorldPacketValidator : PacketValidator<EnterWorldPacket>
{
    public override bool TryValidate(EnterWorldPacket packet, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(packet, out var code))
        {
            errorPacket = new EnterWorldResultPacket
            {
                Success = false,
                Code = code
            };
            return false;
        }

        if (packet.CharacterId == Guid.Empty)
        {
            errorPacket = new EnterWorldResultPacket
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
