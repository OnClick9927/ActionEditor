using System;
using System.Globalization;
#if !ENABLE_IL2CPP
using System.Linq.Expressions;
#endif

namespace ActionBuffer
{
    public abstract class AtomicBuffConverter<T> : BuffConverter<T>
    {
        protected override void OnScan(BufferScan scan, T value) { }
    }

    internal sealed class BoolConverter : AtomicBuffConverter<bool>
    {
        protected override bool OnRead(IBufferReader reader, Type type) => reader.ReadBool();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, bool value) =>
            writer.WriteBool(value);
    }

    internal sealed class ByteConverter : AtomicBuffConverter<byte>
    {
        protected override byte OnRead(IBufferReader reader, Type type) => reader.ReadByte();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, byte value) =>
            writer.WriteByte(value);
    }

    internal sealed class SByteConverter : AtomicBuffConverter<sbyte>
    {
        protected override sbyte OnRead(IBufferReader reader, Type type) =>
            unchecked((sbyte)reader.ReadByte());
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, sbyte value) =>
            writer.WriteByte(unchecked((byte)value));
    }

    internal sealed class CharConverter : AtomicBuffConverter<char>
    {
        protected override char OnRead(IBufferReader reader, Type type) => reader.ReadChar();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, char value) =>
            writer.WriteChar(value);
    }

    internal sealed class ShortConverter : AtomicBuffConverter<short>
    {
        protected override short OnRead(IBufferReader reader, Type type) => reader.ReadInt16();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, short value) =>
            writer.WriteInt16(value);
    }

    internal sealed class UShortConverter : AtomicBuffConverter<ushort>
    {
        protected override ushort OnRead(IBufferReader reader, Type type) => reader.ReadUInt16();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, ushort value) =>
            writer.WriteUInt16(value);
    }

    internal sealed class IntConverter : AtomicBuffConverter<int>
    {
        protected override int OnRead(IBufferReader reader, Type type) => reader.ReadInt32();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, int value) =>
            writer.WriteInt32(value);
    }

    internal sealed class UIntConverter : AtomicBuffConverter<uint>
    {
        protected override uint OnRead(IBufferReader reader, Type type) => reader.ReadUInt32();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, uint value) =>
            writer.WriteUInt32(value);
    }

    internal sealed class LongConverter : AtomicBuffConverter<long>
    {
        protected override long OnRead(IBufferReader reader, Type type) => reader.ReadInt64();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, long value) =>
            writer.WriteInt64(value);
    }

    internal sealed class ULongConverter : AtomicBuffConverter<ulong>
    {
        protected override ulong OnRead(IBufferReader reader, Type type) => reader.ReadUInt64();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, ulong value) =>
            writer.WriteUInt64(value);
    }

    internal sealed class FloatConverter : AtomicBuffConverter<float>
    {
        protected override float OnRead(IBufferReader reader, Type type) => reader.ReadFloat();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, float value) =>
            writer.WriteFloat(value);
    }

    internal sealed class DoubleConverter : AtomicBuffConverter<double>
    {
        protected override double OnRead(IBufferReader reader, Type type) => reader.ReadDouble();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, double value) =>
            writer.WriteDouble(value);
    }

    internal sealed class DecimalConverter : AtomicBuffConverter<decimal>
    {
        protected override decimal OnRead(IBufferReader reader, Type type) =>
            decimal.Parse(reader.ReadUTF8(), NumberStyles.Number, CultureInfo.InvariantCulture);
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, decimal value) =>
            writer.WriteUTF8(value.ToString(CultureInfo.InvariantCulture));
    }

    internal sealed class StringConverter : AtomicBuffConverter<string>
    {
        protected override void OnScan(BufferScan scan, string value)
        {
            int limit = scan.MaxScalarLength;
            if (value != null && value.Length > limit)
                throw new FormatException($"String length cannot exceed {limit} characters.");
        }

        protected override string OnRead(IBufferReader reader, Type type) => reader.ReadUTF8();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, string value) =>
            writer.WriteUTF8(value);
    }

    internal sealed class GuidConverter : AtomicBuffConverter<Guid>
    {
        protected override Guid OnRead(IBufferReader reader, Type type) => reader.ReadGuid();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, Guid value) =>
            writer.WriteGuid(value);
    }

    internal sealed class DateTimeConverter : AtomicBuffConverter<DateTime>
    {
        protected override DateTime OnRead(IBufferReader reader, Type type) =>
            DateTime.FromBinary(reader.ReadInt64());
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, DateTime value) =>
            writer.WriteInt64(value.ToBinary());
    }

    internal sealed class TimeSpanConverter : AtomicBuffConverter<TimeSpan>
    {
        protected override TimeSpan OnRead(IBufferReader reader, Type type) =>
            TimeSpan.FromTicks(reader.ReadInt64());
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TimeSpan value) =>
            writer.WriteInt64(value.Ticks);
    }

    internal sealed class EnumConverter<T> : AtomicBuffConverter<T> where T : struct, Enum
    {
        protected override T OnRead(IBufferReader reader, Type type)
        {
            if (reader is ITypedEnumReader typed) return typed.ReadEnumValue<T>();
            return (T)(object)reader.ReadEnum(type);
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T value)
        {
            if (writer is ITypedEnumWriter typed)
                typed.WriteEnumValue(value);
            else
                writer.WriteEnum((Enum)(object)value);
        }
    }

    internal static class EnumValue<T> where T : struct, Enum
    {
#if ENABLE_IL2CPP
        private static readonly bool Signed = IsSigned();
        internal static ulong ToUInt64(T value) => Signed
            ? unchecked((ulong)Convert.ToInt64(value))
            : Convert.ToUInt64(value);
        internal static T FromUInt64(ulong value) => (T)Enum.ToObject(typeof(T), value);

        private static bool IsSigned()
        {
            var type = Enum.GetUnderlyingType(typeof(T));
            return type == typeof(sbyte) || type == typeof(short) || type == typeof(int) ||
                   type == typeof(long);
        }
#else
        private static readonly Func<T, ulong> ToUInt64Converter = CreateToUInt64();
        private static readonly Func<ulong, T> FromUInt64Converter = CreateFromUInt64();

        internal static ulong ToUInt64(T value) => ToUInt64Converter(value);
        internal static T FromUInt64(ulong value) => FromUInt64Converter(value);

        private static Func<T, ulong> CreateToUInt64()
        {
            var value = Expression.Parameter(typeof(T), "value");
            var underlying = Expression.Convert(value, Enum.GetUnderlyingType(typeof(T)));
            return Expression.Lambda<Func<T, ulong>>(
                Expression.Convert(underlying, typeof(ulong)), value).Compile();
        }

        private static Func<ulong, T> CreateFromUInt64()
        {
            var value = Expression.Parameter(typeof(ulong), "value");
            var underlying = Expression.Convert(value, Enum.GetUnderlyingType(typeof(T)));
            return Expression.Lambda<Func<ulong, T>>(
                Expression.Convert(underlying, typeof(T)), value).Compile();
        }
#endif
    }

    internal sealed class NullableConverter<T> : BuffConverter<T?> where T : struct
    {
        protected override void OnScan(BufferScan scan, T? value)
        {
            if (value.HasValue) ConverterCache<T>.Get(scan).ScanValue(scan, value.Value);
        }

        protected override T? OnRead(IBufferReader reader, Type type) =>
            reader.ReadNullable(ConverterCache<T>.Get(reader));
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T? value) =>
            writer.WriteNullable(scan, value, ConverterCache<T>.Get(scan));
    }
}
