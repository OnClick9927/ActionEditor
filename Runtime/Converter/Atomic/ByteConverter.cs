using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(byte))]
    class ByteConverter : AtomicBuffConverter<byte>
    {
        protected override byte OnRead(IBufferReader reader, Type type) => reader.ReadByte();
        protected override void OnWrite(IBufferWriter writer, byte value) => writer.WriteByte(value);
    }
}
