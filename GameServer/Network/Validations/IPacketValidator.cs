using System.ComponentModel.DataAnnotations;
using System.Reflection;
using GameShared.Attributes;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public interface IPacketValidator
{
    Type PacketType { get; }
    bool TryValidate(IPacket packet, out IPacket? errorPacket);
}

public interface IPacketValidator<in TPacket> : IPacketValidator
    where TPacket : IPacket
{
    bool TryValidate(TPacket packet, out IPacket? errorPacket);
}

public abstract class PacketValidator<TPacket> : IPacketValidator<TPacket>
    where TPacket : class, IPacket
{
    public Type PacketType => typeof(TPacket);

    public bool TryValidate(IPacket packet, out IPacket? errorPacket)
    {
        if (packet is not TPacket typedPacket)
        {
            errorPacket = null;
            return true;
        }

        return TryValidate(typedPacket, out errorPacket);
    }

    protected static bool TryValidateAnnotations(
        TPacket packet,
        out MessageCode errorCode)
    {
        var properties = typeof(TPacket).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            var attributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();
            if (attributes.Length == 0)
                continue;
            var codeAttr = property.GetCustomAttribute<ValidationCodeAttribute>(inherit: true);

            var value = property.GetValue(packet);
            var context = new ValidationContext(packet)
            {
                MemberName = property.Name
            };

            foreach (var attribute in attributes)
            {
                var validationResult = attribute.GetValidationResult(value, context);
                if (validationResult == ValidationResult.Success)
                    continue;

                errorCode = codeAttr?.Code ?? MessageCode.ValidationFailed;
                return false;
            }
        }

        errorCode = MessageCode.None;
        return true;
    }

    public abstract bool TryValidate(TPacket packet, out IPacket? errorPacket);
}
