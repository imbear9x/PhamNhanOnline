using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public sealed class CreateCharacterPacketValidator : PacketValidator<CreateCharacterPacket>
{
    public override bool TryValidate(CreateCharacterPacket packet, out IPacket? errorPacket)
    {
        if (!TryValidateAnnotations(packet, out var code))
        {
            errorPacket = new CreateCharacterResultPacket
            {
                Success = false,
                Code = code
            };
            return false;
        }

        if (!IsValidCharacterName(packet.Name!))
        {
            errorPacket = new CreateCharacterResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterNameInvalid
            };
            return false;
        }

        errorPacket = null;
        return true;
    }

    private static bool IsValidCharacterName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        name = name.Trim();
        if (name.Length is < 3 or > 20)
            return false;

        var hasLetterOrDigit = false;
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch))
            {
                hasLetterOrDigit = true;
                continue;
            }

            if (ch == ' ' || ch == '_')
                continue;

            return false;
        }

        return hasLetterOrDigit;
    }
}
