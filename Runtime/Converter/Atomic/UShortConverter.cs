using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(ushort))]
    class UShortConverter : AtomicBuffConverter<ushort>
    {
        protected override ushort OnRead(IBufferReader reader, Type type) => reader.ReadUInt16();
        protected override void OnWrite(IBufferWriter writer, ushort value) => writer.WriteUInt16(value);
    }
}
