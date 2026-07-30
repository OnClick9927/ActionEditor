using System;

namespace ActionBuffer
{
    internal sealed class NullableConverter<T> : BuffConverter<T?> where T : struct
    {
        private BuffConverter<T> _converter;
        private int _converterVersion = -1;
        private readonly Func<IBufferReader, T> _readElement;
        private readonly Action<IBufferWriter, BufferScan, T> _writeElement;

        public NullableConverter()
        {
            _readElement = ReadValue;
            _writeElement = WriteValue;
        }

        private BuffConverter<T> Converter
        {
            get
            {
                if (_converterVersion == BufferSerializer.ConverterVersion) return _converter;
                _converter = BufferSerializer.GetConverter<T>();
                _converterVersion = BufferSerializer.ConverterVersion;
                return _converter;
            }
        }

        protected override void OnScan(BufferScan scan, T? value)
        {
            if (value.HasValue)
                Converter.ScanValue(scan, value.Value);
        }

        protected override T? OnRead(IBufferReader reader, Type type) =>
            reader.ReadNullable(_readElement);

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T? value) =>
            writer.WriteNullable(scan, value, _writeElement);

        private T ReadValue(IBufferReader reader) => Converter.ReadValue(reader, typeof(T));

        private void WriteValue(IBufferWriter writer, BufferScan scan, T value) =>
            Converter.WriteValue(writer, scan, value);
    }
}
