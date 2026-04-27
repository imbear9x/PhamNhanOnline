using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using GameShared.Attributes;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Validations;

public static class PacketAnnotationValidation
{
    private static readonly ConcurrentDictionary<Type, ValidatedProperty[]> PropertiesByPacketType = new();

    public static bool TryValidate(IPacket packet, out IPacket? errorPacket)
    {
        var packetType = packet.GetType();
        var properties = PropertiesByPacketType.GetOrAdd(packetType, ResolveValidatedProperties);
        if (properties.Length == 0)
        {
            errorPacket = null;
            return true;
        }

        foreach (var property in properties)
        {
            var value = property.Property.GetValue(packet);
            var context = new ValidationContext(packet)
            {
                MemberName = property.Property.Name
            };

            foreach (var attribute in property.Attributes)
            {
                var validationResult = attribute.GetValidationResult(value, context);
                if (validationResult == ValidationResult.Success)
                    continue;

                errorPacket = CreateErrorPacket(packet, property.ErrorCode);
                return false;
            }
        }

        errorPacket = null;
        return true;
    }

    private static ValidatedProperty[] ResolveValidatedProperties(Type packetType)
    {
        return packetType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => new ValidatedProperty(
                property,
                property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray(),
                property.GetCustomAttribute<ValidationCodeAttribute>(inherit: true)?.Code ?? MessageCode.ValidationFailed))
            .Where(static property => property.Attributes.Length > 0)
            .ToArray();
    }

    private static IPacket? CreateErrorPacket(IPacket requestPacket, MessageCode errorCode)
    {
        var resultType = ResolveResultPacketType(requestPacket.GetType());
        if (resultType is null || !typeof(IPacket).IsAssignableFrom(resultType))
            return null;

        if (Activator.CreateInstance(resultType) is not IPacket resultPacket)
            return null;

        CopyMatchingProperties(requestPacket, resultPacket);
        TrySetProperty(resultPacket, "Success", false);
        TrySetProperty(resultPacket, "Code", errorCode);
        return resultPacket;
    }

    private static Type? ResolveResultPacketType(Type packetType)
    {
        const string packetSuffix = "Packet";
        if (!packetType.Name.EndsWith(packetSuffix, StringComparison.Ordinal))
            return null;

        var resultName = packetType.Name[..^packetSuffix.Length] + "ResultPacket";
        return packetType.Assembly.GetType($"{packetType.Namespace}.{resultName}");
    }

    private static void CopyMatchingProperties(IPacket requestPacket, IPacket resultPacket)
    {
        var resultProperties = resultPacket
            .GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanWrite)
            .ToDictionary(static property => property.Name, StringComparer.Ordinal);

        foreach (var sourceProperty in requestPacket.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!sourceProperty.CanRead || !resultProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
                continue;

            TrySetProperty(resultPacket, targetProperty, sourceProperty.GetValue(requestPacket));
        }
    }

    private static void TrySetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
            return;

        TrySetProperty(target, property, value);
    }

    private static void TrySetProperty(object target, PropertyInfo property, object? value)
    {
        if (!property.CanWrite || !CanAssignValue(property.PropertyType, value))
            return;

        property.SetValue(target, value);
    }

    private static bool CanAssignValue(Type targetType, object? value)
    {
        if (value is null)
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;

        var assignableTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return assignableTargetType.IsInstanceOfType(value);
    }

    private sealed record ValidatedProperty(
        PropertyInfo Property,
        ValidationAttribute[] Attributes,
        MessageCode ErrorCode);
}
