using System;
using System.Collections.Generic;


namespace ActionBuffer
{
    public interface IBufferWriter
    {
        bool CollectMeta { get; }
        bool FullField { get; }
        void Init();
        void WriteIEnumerable<T>(BufferScan scan, IEnumerable<T> values, Action<IBufferWriter, BufferScan, T> write);
        void WriteNullable<T>(BufferScan scan, T? value, Action<IBufferWriter, BufferScan, T> write) where T : struct;
        void WriteKeyValuePair<TKey, TValue>(BufferScan scan, KeyValuePair<TKey, TValue> value,
            Action<IBufferWriter, BufferScan, TKey> writeKey,
            Action<IBufferWriter, BufferScan, TValue> writeValue) =>
            throw new NotSupportedException("This writer does not support KeyValuePair values.");

        void WriteBool(bool value);
        void WriteByte(byte value);
        void WriteChar(char value);
        void WriteDouble(double value);
        void WriteEnum(Enum data);
        void WriteFloat(float value);
        void WriteInt16(short value);
        void WriteInt32(int value);
        void WriteInt64(long value);
        void WriteObject<T>(BufferScan scan, T value) =>
            WriteObject(scan, value, value == null ? null : TypeHelper.GetTypeFields(value.GetType()));
        void WriteObject<T>(BufferScan scan, T value, TypeHelper.TypeFields fields);

        void WriteUInt16(ushort value);
        void WriteUInt32(uint value);
        void WriteUInt64(ulong value);
        void WriteUTF8(string value);
        void WriteGuid(Guid value) => WriteUTF8(value.ToString("D"));
    }
}
