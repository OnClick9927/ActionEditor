using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(int))]
    class IntConverter : AtomicBuffConverter<int>
    {
        protected override int OnRead(IBufferReader reader, Type type) => reader.ReadInt32();
        protected override void OnWrite(IBufferWriter writer, int value) => writer.WriteInt32(value);
    }
}
