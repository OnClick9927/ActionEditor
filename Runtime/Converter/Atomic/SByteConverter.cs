using System;

namespace ActionBuffer
{
    internal sealed class SByteConverter : AtomicBuffConverter<sbyte>
    {
        protected override sbyte OnRead(IBufferReader reader, Type type) => unchecked((sbyte)reader.ReadByte());
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, sbyte value) =>
            writer.WriteByte(unchecked((byte)value));
    }
}
