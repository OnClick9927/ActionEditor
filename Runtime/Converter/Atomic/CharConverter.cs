using System;

namespace ActionBuffer
{
    class CharConverter : AtomicBuffConverter<char>
    {
        protected override char OnRead(IBufferReader reader, Type type) => reader.ReadChar();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, char value) => writer.WriteChar(value);
    }
}
