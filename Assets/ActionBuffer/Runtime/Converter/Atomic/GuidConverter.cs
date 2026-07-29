using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(Guid))]
    class GuidConverter : AtomicBuffConverter<Guid>
    {
        protected override Guid OnRead(IBufferReader reader, Type type) => Guid.Parse(reader.ReadUTF8());
        protected override void OnWrite(IBufferWriter writer, Guid value) => writer.WriteUTF8(value.ToString());
    }
}
