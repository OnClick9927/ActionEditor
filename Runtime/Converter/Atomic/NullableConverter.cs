using System;

namespace ActionBuffer
{
    internal sealed class NullableConverter<T> : BuffConverter<T?> where T : struct
    {
        private BuffConverter<T> _converter;
        private long _converterVersion = -1;
        private readonly Func<IBufferReader, T> _readElement;
        private readonly Action<IBufferWriter, BufferScan, T> _writeElement;

        public NullableConverter()
        {
            _readElement = ReadValue;
            _writeElement = WriteValue;
        }

        private BuffConverter<T> GetConverter(BufferSerializerSettings settings)
        {
            long version = BufferSerializer.GetResolverVersion(settings);
            if (_converterVersion == version) return _converter;
            _converter = BufferSerializer.GetConverter<T>(settings);
            _converterVersion = version;
            return _converter;
        }

        protected override void OnScan(BufferScan scan, T? value)
        {
            if (value.HasValue)
                GetConverter(scan.Settings).ScanValue(scan, value.Value);
        }

        protected override T? OnRead(IBufferReader reader, Type type) =>
            reader.ReadNullable(_readElement);

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T? value) =>
            writer.WriteNullable(scan, value, _writeElement);

        private T ReadValue(IBufferReader reader) =>
            GetConverter(BufferSerializerSettings.DefaultSetting).ReadValue(reader, typeof(T));

        private void WriteValue(IBufferWriter writer, BufferScan scan, T value) =>
            GetConverter(scan.Settings).WriteValue(writer, scan, value);
    }
}
