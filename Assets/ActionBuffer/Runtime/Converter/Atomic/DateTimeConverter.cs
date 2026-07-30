using System;

namespace ActionBuffer
{
    class DateTimeConverter : AtomicBuffConverter<DateTime>
    {
        private static BuffConverter<long> _converter;
        private static int _converterVersion = -1;
        private static BuffConverter<long> Converter
        {
            get
            {
                if (_converterVersion == BufferSerializer.ConverterVersion) return _converter;
                _converter = BufferSerializer.GetConverter<long>();
                _converterVersion = BufferSerializer.ConverterVersion;
                return _converter;
            }
        }
        protected override DateTime OnRead(IBufferReader reader, Type type)
        {
            return DateTime.FromBinary(Converter.ReadValue(reader, typeof(long)));
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, DateTime value)
        {
            Converter.WriteValue(writer, scan, value.ToBinary());
        }
    }
}
