using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(ulong))]
    class ULongConverter : AtomicBuffConverter<ulong>
    {
        protected override ulong OnRead(IBufferReader reader, Type type) => reader.ReadUInt64();
        protected override void OnWrite(IBufferWriter writer, ulong value) => writer.WriteUInt64(value);
    }
}
