using System;

namespace ActionBuffer
{
    class UIntConverter : AtomicBuffConverter<uint>
    {
        protected override uint OnRead(IBufferReader reader, Type type) => reader.ReadUInt32();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, uint value) => writer.WriteUInt32(value);
    }
}
