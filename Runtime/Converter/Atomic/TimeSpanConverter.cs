using System;

namespace ActionBuffer
{
    class TimeSpanConverter : AtomicBuffConverter<TimeSpan>
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
        protected override TimeSpan OnRead(IBufferReader reader, Type type)
        {
            return TimeSpan.FromTicks(Converter.ReadValue(reader, typeof(long)));
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TimeSpan value)
        {
            Converter.WriteValue(writer, scan, value.Ticks);
        }
    }
}
