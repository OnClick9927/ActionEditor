using System;

namespace ActionBuffer
{
    class BoolConverter : AtomicBuffConverter<bool>
    {

        protected override bool OnRead(IBufferReader reader, Type type) => reader.ReadBool();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, bool value) => writer.WriteBool(value);
    }
}
