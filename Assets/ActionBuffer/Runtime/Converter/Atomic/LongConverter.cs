using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(long))]
    class LongConverter : AtomicBuffConverter<long>
    {
        protected override long OnRead(IBufferReader reader, Type type) => reader.ReadInt64();
        protected override void OnWrite(IBufferWriter writer, long value) => writer.WriteInt64(value);
    }
}
