using System;

namespace ActionBuffer
{
    class IntConverter : AtomicBuffConverter<int>
    {
        protected override int OnRead(IBufferReader reader, Type type) => reader.ReadInt32();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, int value) => writer.WriteInt32(value);
    }
}
