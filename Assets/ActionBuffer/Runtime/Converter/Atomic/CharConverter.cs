using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(char))]
    class CharConverter : AtomicBuffConverter<char>
    {
        protected override char OnRead(IBufferReader reader, Type type) => reader.ReadChar();
        protected override void OnWrite(IBufferWriter writer, char value) => writer.WriteChar(value);
    }
}
