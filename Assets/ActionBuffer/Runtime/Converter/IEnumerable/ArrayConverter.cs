using System;
namespace ActionBuffer
{
    class ArrayConverter<T> : IEnumerableConverter<T, T[]>
    {
        protected override T[] OnRead(IBufferReader reader, Type type) => reader.ReadArray(ReadElement);
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
