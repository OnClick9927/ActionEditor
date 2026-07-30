using System;

namespace ActionBuffer
{
    class ShortConverter : AtomicBuffConverter<short>
    {
        protected override short OnRead(IBufferReader reader, Type type) => reader.ReadInt16();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, short value) => writer.WriteInt16(value);
    }
}
