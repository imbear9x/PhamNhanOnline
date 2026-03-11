using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Packets
{
    public static class PacketWriter
    {
        public static void Write(BinaryWriter writer, string value)
            => writer.Write(value ?? string.Empty);

        public static void Write(BinaryWriter writer, int value)
            => writer.Write(value);

        public static void Write(BinaryWriter writer, bool value)
            => writer.Write(value);

        public static void Write(BinaryWriter writer, float value)
            => writer.Write(value);

        public static void Write(BinaryWriter writer, double value)
            => writer.Write(value);

        public static void Write(BinaryWriter writer, long value)
            => writer.Write(value);

        public static void Write(BinaryWriter writer, byte value)
            => writer.Write(value);

        public static void Write(BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        public static void Write(BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }
    }

    public static class PacketReader
    {
        public static string ReadString(BinaryReader reader)
            => reader.ReadString();

        public static int ReadInt(BinaryReader reader)
            => reader.ReadInt32();

        public static bool ReadBool(BinaryReader reader)
            => reader.ReadBoolean();

        public static float ReadFloat(BinaryReader reader)
            => reader.ReadSingle();

        public static double ReadDouble(BinaryReader reader)
            => reader.ReadDouble();

        public static long ReadLong(BinaryReader reader)
            => reader.ReadInt64();

        public static byte ReadByte(BinaryReader reader)
            => reader.ReadByte();

        public static Guid ReadGuid(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(16);
            return new Guid(bytes);
        }

        public static Vector3 ReadVector3(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }
    }

    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
