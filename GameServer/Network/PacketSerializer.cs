using System.IO;
using System.Text;
using GameServer.Network.Packets;

namespace GameServer.Network;

public static class PacketSerializer
{
    public static byte[] Serialize(IPacket packet)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        writer.Write((byte)packet.PacketType);

        switch (packet.PacketType)
        {
            case PacketType.Register:
            {
                var p = (RegisterPacket)packet;
                writer.Write(p.Username ?? string.Empty);
                writer.Write(p.Password ?? string.Empty);
                writer.Write(p.Email    ?? string.Empty);
                break;
            }

            case PacketType.RegisterResult:
            {
                var p = (RegisterResultPacket)packet;
                writer.Write(p.Success);
                writer.Write(p.Error ?? string.Empty);
                break;
            }

            case PacketType.Login:
            {
                var p = (LoginPacket)packet;
                writer.Write(p.Username ?? string.Empty);
                writer.Write(p.Password ?? string.Empty);
                break;
            }

            case PacketType.LoginResult:
            {
                var p = (LoginResultPacket)packet;
                writer.Write(p.Success);
                writer.Write(p.Error ?? string.Empty);
                writer.Write(p.AccountId.ToByteArray());
                break;
            }
        }

        writer.Flush();
        return ms.ToArray();
    }

    public static IPacket? Deserialize(byte[] data)
    {
        if (data.Length == 0)
            return null;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

        var type = (PacketType)reader.ReadByte();

        switch (type)
        {
            case PacketType.Register:
            {
                var username = reader.ReadString();
                var password = reader.ReadString();
                var email    = reader.ReadString();
                return new RegisterPacket
                {
                    Username = username,
                    Password = password,
                    Email    = email
                };
            }

            case PacketType.RegisterResult:
            {
                var success = reader.ReadBoolean();
                var error   = reader.ReadString();
                return new RegisterResultPacket
                {
                    Success = success,
                    Error   = error
                };
            }

            case PacketType.Login:
            {
                var username = reader.ReadString();
                var password = reader.ReadString();
                return new LoginPacket
                {
                    Username = username,
                    Password = password
                };
            }

            case PacketType.LoginResult:
            {
                var success = reader.ReadBoolean();
                var error   = reader.ReadString();
                var bytes   = reader.ReadBytes(16);
                var accountId = new Guid(bytes);

                return new LoginResultPacket
                {
                    Success   = success,
                    Error     = error,
                    AccountId = accountId
                };
            }

            default:
                return null;
        }
    }
}

