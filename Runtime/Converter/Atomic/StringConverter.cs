using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(string))]
    class StringConverter : AtomicBuffConverter<string>
    {
        protected override string OnRead(IBufferReader reader, Type type) => reader.ReadUTF8();
        protected override void OnWrite(IBufferWriter writer, string value) => writer.WriteUTF8(value);
    }
}
