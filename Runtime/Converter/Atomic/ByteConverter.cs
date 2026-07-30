using System;

namespace ActionBuffer
{
    class ByteConverter : AtomicBuffConverter<byte>
    {
        protected override byte OnRead(IBufferReader reader, Type type) => reader.ReadByte();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, byte value) => writer.WriteByte(value);
    }
}
