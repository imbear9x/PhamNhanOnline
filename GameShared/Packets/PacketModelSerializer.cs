using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using GameShared.Attributes;

namespace GameShared.Packets;

public static class PacketModelSerializer
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> MetadataCache = new();
    private static readonly Type PacketModelAttributeType = typeof(PacketModelAttribute);

    public static T? Read<T>(BinaryReader reader)
    {
        return (T?)ReadValue(reader, typeof(T));
    }

    public static void Write<T>(BinaryWriter writer, T value)
    {
        WriteValue(writer, typeof(T), value);
    }

    public static List<T>? ReadList<T>(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count < 0)
            return null;

        var result = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            var value = ReadValue(reader, typeof(T));
            result.Add((T)value!);
        }

        return result;
    }

    public static void WriteList<T>(BinaryWriter writer, IEnumerable<T>? list)
    {
        if (list is null)
        {
            writer.Write(-1);
            return;
        }

        if (list is ICollection<T> typedCollection)
        {
            writer.Write(typedCollection.Count);
            foreach (var item in typedCollection)
                WriteValue(writer, typeof(T), item);
            return;
        }

        var materialized = list as List<T> ?? list.ToList();
        writer.Write(materialized.Count);
        foreach (var item in materialized)
            WriteValue(writer, typeof(T), item);
    }

    public static T[]? ReadArray<T>(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count < 0)
            return null;

        var result = new T[count];
        for (var i = 0; i < count; i++)
        {
            var value = ReadValue(reader, typeof(T));
            result[i] = (T)value!;
        }

        return result;
    }

    public static void WriteArray<T>(BinaryWriter writer, T[]? array)
    {
        if (array is null)
        {
            writer.Write(-1);
            return;
        }

        writer.Write(array.Length);
        foreach (var item in array)
            WriteValue(writer, typeof(T), item);
    }

    private static object? ReadValue(BinaryReader reader, Type type)
    {
        if (TryReadKnownSimple(reader, type, out var value))
            return value;

        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
        {
            var hasValue = reader.ReadBoolean();
            if (!hasValue)
                return null;
            return ReadValue(reader, nullableUnderlying);
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var count = reader.ReadInt32();
            if (count < 0)
                return null;

            var array = Array.CreateInstance(elementType, count);
            for (var i = 0; i < count; i++)
            {
                var element = ReadValue(reader, elementType);
                array.SetValue(element, i);
            }

            return array;
        }

        if (TryGetListElementType(type, out var listElementType))
        {
            var count = reader.ReadInt32();
            if (count < 0)
                return null;

            var listType = typeof(List<>).MakeGenericType(listElementType!);
            var list = (IList)Activator.CreateInstance(listType)!;
            for (var i = 0; i < count; i++)
            {
                list.Add(ReadValue(reader, listElementType!));
            }

            return list;
        }

        var metadata = GetMetadata(type);
        if (!type.IsValueType)
        {
            var hasValue = reader.ReadBoolean();
            if (!hasValue)
                return null;
        }

        var instance = Activator.CreateInstance(type);
        if (instance is null)
            throw new InvalidOperationException($"Cannot create instance for packet model type '{type.FullName}'.");

        foreach (var member in metadata.Members)
        {
            var memberValue = ReadValue(reader, member.MemberType);
            member.SetValue(instance, memberValue);
        }

        return instance;
    }

    private static void WriteValue(BinaryWriter writer, Type type, object? value)
    {
        if (TryWriteKnownSimple(writer, type, value))
            return;

        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
        {
            if (value is null)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            WriteValue(writer, nullableUnderlying, value);
            return;
        }

        if (type.IsArray)
        {
            if (value is null)
            {
                writer.Write(-1);
                return;
            }

            var array = (Array)value;
            var elementType = type.GetElementType()!;
            writer.Write(array.Length);

            for (var i = 0; i < array.Length; i++)
                WriteValue(writer, elementType, array.GetValue(i));
            return;
        }

        if (TryGetListElementType(type, out var listElementType))
        {
            if (value is null)
            {
                writer.Write(-1);
                return;
            }

            var enumerable = (IEnumerable)value;
            var materialized = new List<object?>();
            foreach (var item in enumerable)
                materialized.Add(item);

            writer.Write(materialized.Count);
            foreach (var item in materialized)
                WriteValue(writer, listElementType!, item);
            return;
        }

        var metadata = GetMetadata(type);
        if (!type.IsValueType)
        {
            if (value is null)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
        }

        var model = value ?? throw new InvalidOperationException($"Packet model '{type.FullName}' cannot be null.");
        foreach (var member in metadata.Members)
        {
            WriteValue(writer, member.MemberType, member.GetValue(model));
        }
    }

    private static TypeMetadata GetMetadata(Type type)
    {
        return MetadataCache.GetOrAdd(type, static t =>
        {
            if (!Attribute.IsDefined(t, PacketModelAttributeType, inherit: true))
            {
                throw new InvalidOperationException(
                    $"Type '{t.FullName}' is not marked with [PacketModel], cannot use packet model serialization.");
            }

            var members = new List<MemberMetadata>();
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(static field => !field.IsStatic);
            foreach (var field in fields)
            {
                members.Add(new MemberMetadata(field.Name, field.FieldType, field.GetValue, field.SetValue));
            }

            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(static property =>
                    property.GetMethod is not null &&
                    !property.GetMethod.IsStatic &&
                    property.SetMethod is not null &&
                    property.GetIndexParameters().Length == 0);

            foreach (var property in properties)
            {
                members.Add(new MemberMetadata(property.Name, property.PropertyType, property.GetValue, property.SetValue));
            }

            members.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
            return new TypeMetadata(t, members);
        });
    }

    private static bool TryReadKnownSimple(BinaryReader reader, Type type, out object? value)
    {
        value = null;

        if (type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(type);
            var raw = ReadKnownSimple(reader, underlying);
            value = Enum.ToObject(type, raw!);
            return true;
        }

        if (type == typeof(string))
        {
            value = PacketReader.ReadString(reader);
            return true;
        }

        if (type == typeof(int))
        {
            value = PacketReader.ReadInt(reader);
            return true;
        }

        if (type == typeof(bool))
        {
            value = PacketReader.ReadBool(reader);
            return true;
        }

        if (type == typeof(float))
        {
            value = PacketReader.ReadFloat(reader);
            return true;
        }

        if (type == typeof(double))
        {
            value = PacketReader.ReadDouble(reader);
            return true;
        }

        if (type == typeof(long))
        {
            value = PacketReader.ReadLong(reader);
            return true;
        }

        if (type == typeof(byte))
        {
            value = PacketReader.ReadByte(reader);
            return true;
        }

        if (type == typeof(short))
        {
            value = reader.ReadInt16();
            return true;
        }

        if (type == typeof(ushort))
        {
            value = reader.ReadUInt16();
            return true;
        }

        if (type == typeof(uint))
        {
            value = reader.ReadUInt32();
            return true;
        }

        if (type == typeof(ulong))
        {
            value = reader.ReadUInt64();
            return true;
        }

        if (type == typeof(char))
        {
            value = reader.ReadChar();
            return true;
        }

        if (type == typeof(decimal))
        {
            value = reader.ReadDecimal();
            return true;
        }

        if (type == typeof(Guid))
        {
            value = PacketReader.ReadGuid(reader);
            return true;
        }

        return false;
    }

    private static object? ReadKnownSimple(BinaryReader reader, Type type)
    {
        if (!TryReadKnownSimple(reader, type, out var value))
            throw new InvalidOperationException($"Unsupported simple type '{type.FullName}'.");
        return value;
    }

    private static bool TryWriteKnownSimple(BinaryWriter writer, Type type, object? value)
    {
        if (type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(type);
            var raw = Convert.ChangeType(value!, underlying);
            return TryWriteKnownSimple(writer, underlying, raw);
        }

        if (type == typeof(string))
        {
            PacketWriter.Write(writer, (string?)value ?? string.Empty);
            return true;
        }

        if (type == typeof(int))
        {
            PacketWriter.Write(writer, (int)value!);
            return true;
        }

        if (type == typeof(bool))
        {
            PacketWriter.Write(writer, (bool)value!);
            return true;
        }

        if (type == typeof(float))
        {
            PacketWriter.Write(writer, (float)value!);
            return true;
        }

        if (type == typeof(double))
        {
            PacketWriter.Write(writer, (double)value!);
            return true;
        }

        if (type == typeof(long))
        {
            PacketWriter.Write(writer, (long)value!);
            return true;
        }

        if (type == typeof(byte))
        {
            PacketWriter.Write(writer, (byte)value!);
            return true;
        }

        if (type == typeof(short))
        {
            writer.Write((short)value!);
            return true;
        }

        if (type == typeof(ushort))
        {
            writer.Write((ushort)value!);
            return true;
        }

        if (type == typeof(uint))
        {
            writer.Write((uint)value!);
            return true;
        }

        if (type == typeof(ulong))
        {
            writer.Write((ulong)value!);
            return true;
        }

        if (type == typeof(char))
        {
            writer.Write((char)value!);
            return true;
        }

        if (type == typeof(decimal))
        {
            writer.Write((decimal)value!);
            return true;
        }

        if (type == typeof(Guid))
        {
            PacketWriter.Write(writer, (Guid)value!);
            return true;
        }

        return false;
    }

    private static bool TryGetListElementType(Type type, out Type? elementType)
    {
        elementType = null;

        if (!type.IsGenericType)
            return false;

        var generic = type.GetGenericTypeDefinition();
        if (generic != typeof(List<>) &&
            generic != typeof(IList<>) &&
            generic != typeof(IReadOnlyList<>) &&
            generic != typeof(ICollection<>) &&
            generic != typeof(IEnumerable<>))
        {
            return false;
        }

        elementType = type.GetGenericArguments()[0];
        return true;
    }

    private sealed class MemberMetadata
    {
        public MemberMetadata(
            string name,
            Type memberType,
            Func<object, object?> getValue,
            Action<object, object?> setValue)
        {
            Name = name;
            MemberType = memberType;
            GetValue = getValue;
            SetValue = setValue;
        }

        public string Name { get; }
        public Type MemberType { get; }
        public Func<object, object?> GetValue { get; }
        public Action<object, object?> SetValue { get; }
    }

    private sealed class TypeMetadata
    {
        public TypeMetadata(Type type, IReadOnlyList<MemberMetadata> members)
        {
            Type = type;
            Members = members;
        }

        public Type Type { get; }
        public IReadOnlyList<MemberMetadata> Members { get; }
    }
}
