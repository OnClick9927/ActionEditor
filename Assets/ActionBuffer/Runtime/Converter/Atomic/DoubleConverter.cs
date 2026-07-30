using System;

namespace ActionBuffer
{
    class DoubleConverter : AtomicBuffConverter<double>
    {
        protected override double OnRead(IBufferReader reader, Type type) => reader.ReadDouble();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, double value) => writer.WriteDouble(value);
    }
}
