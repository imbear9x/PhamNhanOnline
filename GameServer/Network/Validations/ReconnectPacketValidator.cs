using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class ReconnectPacketValidator : PacketValidator<ReconnectPacket>
{
    public override bool TryValidate(ReconnectPacket reconnectPacket, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(reconnectPacket, out _))
        {
            errorPacket = new ReconnectResultPacket
            {
                Success = false,
                Code = MessageCode.ReconnectTokenInvalid,
                AccountId = Guid.Empty
            };
            return false;
        }

        errorPacket = null;
        return true;
    }
}
