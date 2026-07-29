using System;
using System.Collections.Generic;


namespace ActionBuffer
{
    public interface IBufferWriter
    {
        bool CollectMeta { get; }
        bool FullField { get; }
        void Init(BufferScan scan);
        void WriteIEnumerable<T>(IEnumerable<T> values, Action<IBufferWriter, T> write);

        void WriteBool(bool value);
        void WriteByte(byte value);
        void WriteChar(char value);
        void WriteDouble(double value);
        void WriteEnum(Enum data);
        void WriteFloat(float value);
        void WriteInt16(short value);
        void WriteInt32(int value);
        void WriteInt64(long value);
        void WriteObject<T>(T value) => WriteObject(value, value == null ? null : TypeHelper.GetTypeFields(value.GetType()));
        void WriteObject<T>(T value, TypeHelper.TypeFields fields);

        void WriteUInt16(ushort value);
        void WriteUInt32(uint value);
        void WriteUInt64(ulong value);
        void WriteUTF8(string value);
    }
}
