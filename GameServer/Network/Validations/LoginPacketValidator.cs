using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class LoginPacketValidator : PacketValidator<LoginPacket>
{
    public override bool TryValidate(LoginPacket loginPacket, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(loginPacket, out var code))
        {
            errorPacket = new LoginResultPacket
            {
                Success = false,
                Code = code,
                AccountId = Guid.Empty
            };
            return false;
        }

        errorPacket = null;
        return true;
    }
}
