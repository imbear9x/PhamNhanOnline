using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class RegisterPacketValidator : PacketValidator<RegisterPacket>
{
    public override bool TryValidate(RegisterPacket registerPacket, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(registerPacket, out var code))
        {
            errorPacket = new RegisterResultPacket
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
