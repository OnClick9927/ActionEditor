using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(float))]
    class FloatConverter : AtomicBuffConverter<float>
    {
        protected override float OnRead(IBufferReader reader, Type type) => reader.ReadFloat();
        protected override void OnWrite(IBufferWriter writer, float value) => writer.WriteFloat(value);
    }
}
