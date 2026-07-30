using System.Collections.Generic;
namespace ActionBuffer
{
    public abstract class IEnumerableConverter<T, V> : BuffConverter<V> where V : IEnumerable<T>
    {
        private BuffConverter<T> _converter;
        private int _converterVersion = -1;
        protected readonly System.Func<IBufferReader, T> ReadElement;
        private readonly System.Action<IBufferWriter, BufferScan, T> _writeElement;

        protected IEnumerableConverter()
        {
            ReadElement = ReadOnce;
            _writeElement = WriteOnce;
        }
        protected BuffConverter<T> ElementConverter
        {
            get
            {
                if (_converterVersion == BufferSerializer.ConverterVersion) return _converter;
                _converter = BufferSerializer.GetConverter<T>();
                _converterVersion = BufferSerializer.ConverterVersion;
                return _converter;
            }
        }
        protected override void OnScan(BufferScan scan, V value)
        {
            if (value is ISet<T>)
            {
                scan.ScanEnumerable(value, ElementConverter, DeterministicComparer<T>.Instance);
                return;
            }
            if (value is System.Collections.IDictionary)
                throw new System.NotSupportedException(
                    $"Dictionary value '{value.GetType()}' must be declared as a supported dictionary interface.");
            scan.ScanEnumerable(value, ElementConverter);
        }
        protected void ScanWithComparer(BufferScan scan, V value, IComparer<T> comparer) =>
            scan.ScanEnumerable(value, ElementConverter, comparer);
        protected T ReadOnce(IBufferReader reader) => ElementConverter.ReadValue(reader, typeof(T));
        private void WriteOnce(IBufferWriter writer, BufferScan scan, T value) =>
            ElementConverter.WriteValue(writer, scan, value);
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, V value) =>
            writer.WriteIEnumerable(scan, value, _writeElement);

    }
}
