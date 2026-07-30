using System;

namespace ActionBuffer
{
    class GuidConverter : AtomicBuffConverter<Guid>
    {
        protected override Guid OnRead(IBufferReader reader, Type type) => reader.ReadGuid();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, Guid value) => writer.WriteGuid(value);
    }
}
