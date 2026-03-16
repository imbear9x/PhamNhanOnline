using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class AllocatePotentialPacketValidator : PacketValidator<AllocatePotentialPacket>
{
    public override bool TryValidate(AllocatePotentialPacket packet, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(packet, out var code))
        {
            errorPacket = new AllocatePotentialResultPacket
            {
                Success = false,
                Code = code
            };
            return false;
        }

        errorPacket = null;
        return true;
    }
}
