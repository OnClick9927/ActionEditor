using System;

namespace ActionBuffer
{
    class DateTimeConverter : AtomicBuffConverter<DateTime>
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
        protected override DateTime OnRead(IBufferReader reader, Type type)
        {
            return DateTime.FromBinary(GetConverter(BufferSerializerSettings.DefaultSetting)
                .ReadValue(reader, typeof(long)));
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, DateTime value)
        {
            GetConverter(scan.Settings).WriteValue(writer, scan, value.ToBinary());
        }
    }
}
