using System;
using System.Globalization;

namespace ActionBuffer
{
    internal sealed class DecimalConverter : AtomicBuffConverter<decimal>
    {
        protected override decimal OnRead(IBufferReader reader, Type type) =>
            decimal.Parse(reader.ReadUTF8(), NumberStyles.Number, CultureInfo.InvariantCulture);

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, decimal value) =>
            writer.WriteUTF8(value.ToString(CultureInfo.InvariantCulture));
    }
}
