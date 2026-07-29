using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(double))]
    class DoubleConverter : AtomicBuffConverter<double>
    {
        protected override double OnRead(IBufferReader reader, Type type) => reader.ReadDouble();
        protected override void OnWrite(IBufferWriter writer, double value) => writer.WriteDouble(value);
    }
}
