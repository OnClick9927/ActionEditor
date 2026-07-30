using System;

namespace ActionBuffer
{
    class EnumConverter<T> : AtomicBuffConverter<T>
    {
        protected override T OnRead(IBufferReader reader, Type type) => (T)(object)reader.ReadEnum(type);
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T value) =>
            writer.WriteEnum((Enum)(object)value);
    }
}
