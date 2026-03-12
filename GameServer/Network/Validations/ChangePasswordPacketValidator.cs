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

        if (!IsValidUsername(packet.Username!))
        {
            errorPacket = new ChangePasswordResultPacket
            {
                Success = false,
                Code = MessageCode.UsernameWrong
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

    private static bool IsValidUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return false;

        if (!IsAsciiLowerLetter(username[0]))
            return false;

        foreach (var c in username)
        {
            if (IsAsciiLowerLetter(c) || char.IsDigit(c) || c == '_')
                continue;

            return false;
        }

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

    private static bool IsAsciiLowerLetter(char c) => c >= 'a' && c <= 'z';
}
