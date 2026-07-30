using System;

namespace ActionBuffer
{
    class UShortConverter : AtomicBuffConverter<ushort>
    {
        protected override ushort OnRead(IBufferReader reader, Type type) => reader.ReadUInt16();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, ushort value) => writer.WriteUInt16(value);
    }
}
