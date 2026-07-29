using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(short))]
    class ShortConverter : AtomicBuffConverter<short>
    {
        protected override short OnRead(IBufferReader reader, Type type) => reader.ReadInt16();
        protected override void OnWrite(IBufferWriter writer, short value) => writer.WriteInt16(value);
    }
}
