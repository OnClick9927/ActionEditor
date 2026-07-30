using System;

namespace ActionBuffer
{
    class LongConverter : AtomicBuffConverter<long>
    {
        protected override long OnRead(IBufferReader reader, Type type) => reader.ReadInt64();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, long value) => writer.WriteInt64(value);
    }
}
