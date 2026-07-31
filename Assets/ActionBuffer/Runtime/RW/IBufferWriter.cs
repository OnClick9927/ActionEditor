using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    public interface IBufferWriter
    {
        bool CollectMeta { get; }
        void Init(BufferScan scan);
        void WriteIEnumerable<T>(BufferScan scan, BuffConverter<T> converter);
        void WriteMultiDimensionalArray<T>(BufferScan scan, int rank,
            BuffConverter<T> converter);
        void WriteNullable<T>(BufferScan scan, T? value, BuffConverter<T> converter)
            where T : struct;
        void WriteKeyValuePair<TKey, TValue>(BufferScan scan,
            KeyValuePair<TKey, TValue> value, BuffConverter<TKey> keyConverter,
            BuffConverter<TValue> valueConverter);

        void WriteBool(bool value);
        void WriteByte(byte value);
        void WriteChar(char value);
        void WriteDouble(double value);
        void WriteEnum(Enum data);
        void WriteFloat(float value);
        void WriteInt16(short value);
        void WriteInt32(int value);
        void WriteInt64(long value);
        void WriteObject<T>(BufferScan scan, T value, TypeHelper.TypeFields fields);
        void WriteUInt16(ushort value);
        void WriteUInt32(uint value);
        void WriteUInt64(ulong value);
        void WriteUTF8(string value);
        void WriteGuid(Guid value);
    }

    internal interface ITypedEnumWriter
    {
        void WriteEnumValue<T>(T value) where T : struct, Enum;
    }
}
