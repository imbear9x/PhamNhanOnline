using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class ChangePasswordPacketValidator : PacketValidator<ChangePasswordPacket>
{
    public override bool TryValidate(ChangePasswordPacket packet, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(packet, out var code))
        {
            errorPacket = new ChangePasswordResultPacket
            {
                Success = false,
                Code = code
            };
            return false;
        }

        if (!IsAsciiPrintablePassword(packet.Password!) || !IsAsciiPrintablePassword(packet.NewPassword!))
        {
            errorPacket = new ChangePasswordResultPacket
            {
                Success = false,
                Code = MessageCode.PasswordWrong
            };
            return false;
        }

        errorPacket = null;
        return true;
    }

    private static bool IsAsciiPrintablePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        foreach (var c in password)
        {
            if (c < '!' || c > '~')
                return false;
        }

        return true;
    }
}
