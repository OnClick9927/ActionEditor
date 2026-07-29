using System.Collections.Generic;
namespace ActionBuffer
{
    public abstract class IEnumerableConverter<T, V> : BuffConverter<V> where V : IEnumerable<T>
    {
        static BuffConverter<T> converter = GetConverter<T>();
        protected override void OnScan(BufferScan scan, V value) => scan.ScanEnumerable(value, converter);
        protected T ReadOnce(IBufferReader reader) => converter.ReadValue(reader, typeof(T));
        private void WriteOnce(IBufferWriter writer, T t) => converter.WriteValue(writer, t);
        protected override void OnWrite(IBufferWriter writer, V value) => writer.WriteIEnumerable(value, WriteOnce);

    }
}
