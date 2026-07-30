using System;

namespace ActionBuffer
{
    class ULongConverter : AtomicBuffConverter<ulong>
    {
        protected override ulong OnRead(IBufferReader reader, Type type) => reader.ReadUInt64();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, ulong value) => writer.WriteUInt64(value);
    }
}
