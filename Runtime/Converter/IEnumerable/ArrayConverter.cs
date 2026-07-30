using System;
namespace ActionBuffer
{
    class ArrayConverter<T> : IEnumerableConverter<T, T[]>
    {
        protected override T[] OnRead(IBufferReader reader, Type type) => reader.ReadArray(ReadElement);
    }

    class Array2DConverter<T> : BuffConverter<T[,]>
    {
        private BuffConverter<T> _converter;
        private int _converterVersion = -1;
        private readonly Func<IBufferReader, T> _readElement;
        private readonly Action<IBufferWriter, BufferScan, T> _writeElement;

        public Array2DConverter()
        {
            _readElement = ReadElement;
            _writeElement = WriteElement;
        }

        private BuffConverter<T> ElementConverter
        {
            get
            {
                if (_converterVersion == BufferSerializer.ConverterVersion) return _converter;
                _converter = BufferSerializer.GetConverter<T>();
                _converterVersion = BufferSerializer.ConverterVersion;
                return _converter;
            }
        }

        protected override void OnScan(BufferScan scan, T[,] value) =>
            scan.ScanArray2D(value, ElementConverter);

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T[,] value) =>
            writer.WriteArray2D(scan, value, _writeElement);

        protected override T[,] OnRead(IBufferReader reader, Type type) =>
            reader.ReadArray2D(_readElement);

        private T ReadElement(IBufferReader reader) => ElementConverter.ReadValue(reader, typeof(T));

        private void WriteElement(IBufferWriter writer, BufferScan scan, T value) =>
            ElementConverter.WriteValue(writer, scan, value);
    }

    class ArraySegmentConverter<T> : IEnumerableConverter<T, ArraySegment<T>>
    {
        protected override void OnScan(BufferScan scan, ArraySegment<T> value)
        {
            if (value.Array == null)
                scan.ScanEnumerable<T>(null, ElementConverter);
            else
                base.OnScan(scan, value);
        }

        protected override ArraySegment<T> OnRead(IBufferReader reader, Type type)
        {
            var values = reader.ReadArray(ReadElement);
            return values == null ? default : new ArraySegment<T>(values);
        }
    }
}
