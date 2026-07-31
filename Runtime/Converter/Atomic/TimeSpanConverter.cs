using System;

namespace ActionBuffer
{
    class TimeSpanConverter : AtomicBuffConverter<TimeSpan>
    {
        private static BuffConverter<long> _converter;
        private static long _converterVersion = -1;
        private static BuffConverter<long> GetConverter(BufferSerializerSettings settings)
        {
            long version = BufferSerializer.GetResolverVersion(settings);
            if (_converterVersion == version) return _converter;
            _converter = BufferSerializer.GetConverter<long>(settings);
            _converterVersion = version;
            return _converter;
        }
        protected override TimeSpan OnRead(IBufferReader reader, Type type)
        {
            return TimeSpan.FromTicks(GetConverter(BufferSerializerSettings.DefaultSetting)
                .ReadValue(reader, typeof(long)));
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TimeSpan value)
        {
            GetConverter(scan.Settings).WriteValue(writer, scan, value.Ticks);
        }
    }
}
