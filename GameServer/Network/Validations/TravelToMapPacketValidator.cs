using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class TravelToMapPacketValidator : PacketValidator<TravelToMapPacket>
{
    public override bool TryValidate(TravelToMapPacket packet, out IPacket? errorPacket)
    {
        if (packet.PortalId.HasValue)
        {
            if (packet.PortalId.Value > 0)
            {
                errorPacket = null;
                return true;
            }

            errorPacket = new TravelToMapResultPacket
            {
                Success = false,
                Code = MessageCode.MapPortalInvalid,
                PortalId = packet.PortalId,
                TargetMapId = packet.TargetMapId
            };
            return false;
        }

        if (packet.TargetMapId.HasValue && packet.TargetMapId.Value > 0)
        {
            errorPacket = null;
            return true;
        }

        errorPacket = new TravelToMapResultPacket
        {
            Success = false,
            Code = MessageCode.MapIdInvalid,
            PortalId = packet.PortalId,
            TargetMapId = packet.TargetMapId
        };
        return false;
    }
}
