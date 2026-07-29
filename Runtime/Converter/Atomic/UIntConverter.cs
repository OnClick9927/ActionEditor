using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(uint))]
    class UIntConverter : AtomicBuffConverter<uint>
    {
        protected override uint OnRead(IBufferReader reader, Type type) => reader.ReadUInt32();
        protected override void OnWrite(IBufferWriter writer, uint value) => writer.WriteUInt32(value);
    }
}
