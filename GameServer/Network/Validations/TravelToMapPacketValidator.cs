using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class TravelToMapPacketValidator : PacketValidator<TravelToMapPacket>
{
    public override bool TryValidate(TravelToMapPacket packet, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(packet, out var code))
        {
            errorPacket = new TravelToMapResultPacket
            {
                Success = false,
                Code = code,
                TargetMapId = packet.TargetMapId
            };
            return false;
        }

        errorPacket = null;
        return true;
    }
}
