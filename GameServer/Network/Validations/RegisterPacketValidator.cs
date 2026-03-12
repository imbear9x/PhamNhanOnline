using GameShared.Packets;
using GameShared.Messages;

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

        if (!IsValidUsername(registerPacket.Username!))
        {
            errorPacket = new RegisterResultPacket
            {
                Success = false,
                Code = MessageCode.UsernameWrong
            };
            return false;
        }

        if (!IsEnglishAlphabetPassword(registerPacket.Password!))
        {
            errorPacket = new RegisterResultPacket
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

    private static bool IsEnglishAlphabetPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        foreach (var ch in password)
        {
            var isLower = ch >= 'a' && ch <= 'z';
            var isUpper = ch >= 'A' && ch <= 'Z';
            if (isLower || isUpper)
                continue;

            return false;
        }

        return true;
    }

    private static bool IsAsciiLowerLetter(char c) => c >= 'a' && c <= 'z';
}
