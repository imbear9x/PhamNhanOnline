using GameShared.Packets;
using GameShared.Messages;

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

        if (!IsValidUsername(loginPacket.Username!))
        {
            errorPacket = new LoginResultPacket
            {
                Success = false,
                Code = MessageCode.UsernameWrong,
                AccountId = Guid.Empty
            };
            return false;
        }

        if (!IsEnglishAlphabetPassword(loginPacket.Password!))
        {
            errorPacket = new LoginResultPacket
            {
                Success = false,
                Code = MessageCode.PasswordWrong,
                AccountId = Guid.Empty
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

        foreach (var c in password)
        {
            var isLower = c >= 'a' && c <= 'z';
            var isUpper = c >= 'A' && c <= 'Z';
            if (isLower || isUpper)
                continue;

            return false;
        }

        return true;
    }

    private static bool IsAsciiLowerLetter(char c) => c >= 'a' && c <= 'z';
}
