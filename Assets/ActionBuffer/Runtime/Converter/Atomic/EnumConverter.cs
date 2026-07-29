using System;

namespace ActionBuffer
{
    class EnumConverter<T> : AtomicBuffConverter<T> where T : Enum
    {
        protected override T OnRead(IBufferReader reader, Type type) => (T)(Enum)reader.ReadEnum(type);
        protected override void OnWrite(IBufferWriter writer, T value) => writer.WriteEnum(value);
    }
}
