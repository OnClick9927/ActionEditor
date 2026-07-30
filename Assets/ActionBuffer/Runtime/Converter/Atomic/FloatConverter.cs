using System;

namespace ActionBuffer
{
    class FloatConverter : AtomicBuffConverter<float>
    {
        protected override float OnRead(IBufferReader reader, Type type) => reader.ReadFloat();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, float value) => writer.WriteFloat(value);
    }
}
