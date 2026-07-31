using System;
namespace ActionBuffer
{
    class ArrayConverter<T> : IEnumerableConverter<T, T[]>
    {
        protected override T[] OnRead(IBufferReader reader, Type type) => reader.ReadArray(ReadElement);
    }

    class MultiDimensionalArrayConverter<TArray, T> : BuffConverter<TArray>
        where TArray : class
    {
        private BuffConverter<T> _converter;
        private long _converterVersion = -1;
        private readonly int _rank = typeof(TArray).GetArrayRank();
        private readonly Func<IBufferReader, T> _readElement;
        private readonly Action<IBufferWriter, BufferScan, T> _writeElement;

        public MultiDimensionalArrayConverter()
        {
            _readElement = ReadElement;
            _writeElement = WriteElement;
        }

        private BuffConverter<T> GetElementConverter(BufferSerializerSettings settings)
        {
            long version = BufferSerializer.GetResolverVersion(settings);
            if (_converterVersion == version) return _converter;
            _converter = BufferSerializer.GetConverter<T>(settings);
            _converterVersion = version;
            return _converter;
        }

        protected override void OnScan(BufferScan scan, TArray value) =>
            scan.ScanMultiDimensionalArray<T>(value as Array, _rank,
                GetElementConverter(scan.Settings));

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TArray value) =>
            writer.WriteMultiDimensionalArray<T>(scan, value as Array, _rank, _writeElement);

        protected override TArray OnRead(IBufferReader reader, Type type) =>
            (TArray)(object)reader.ReadMultiDimensionalArray(_rank, _readElement);

        private T ReadElement(IBufferReader reader) =>
            GetElementConverter(BufferSerializerSettings.DefaultSetting).ReadValue(reader, typeof(T));

        private void WriteElement(IBufferWriter writer, BufferScan scan, T value) =>
            GetElementConverter(scan.Settings).WriteValue(writer, scan, value);
    }

    internal static class MultiDimensionalArrayHelper
    {
        internal static Array Create<T>(BufferScan.ArrayShape shape)
        {
            switch (shape.Rank)
            {
                case 2: return new T[shape.Length0, shape.Length1];
                case 3: return new T[shape.Length0, shape.Length1, shape.Length2];
                case 4: return new T[shape.Length0, shape.Length1, shape.Length2, shape.Length3];
                case 5: return new T[shape.Length0, shape.Length1, shape.Length2, shape.Length3,
                    shape.Length4];
                default: throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }

        internal static void SetValue<T>(Array array, BufferScan.ArrayShape shape,
            int flatIndex, T value)
        {
            int index = flatIndex;
            int i4 = shape.Rank == 5 ? index % shape.Length4 : 0;
            if (shape.Rank == 5) index /= shape.Length4;
            int i3 = shape.Rank >= 4 ? index % shape.Length3 : 0;
            if (shape.Rank >= 4) index /= shape.Length3;
            int i2 = shape.Rank >= 3 ? index % shape.Length2 : 0;
            if (shape.Rank >= 3) index /= shape.Length2;
            int i1 = index % shape.Length1;
            int i0 = index / shape.Length1;

            switch (shape.Rank)
            {
                case 2: ((T[,])array)[i0, i1] = value; break;
                case 3: ((T[,,])array)[i0, i1, i2] = value; break;
                case 4: ((T[,,,])array)[i0, i1, i2, i3] = value; break;
                case 5: ((T[,,,,])array)[i0, i1, i2, i3, i4] = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }
    }

    class ArraySegmentConverter<T> : IEnumerableConverter<T, ArraySegment<T>>
    {
        protected override void OnScan(BufferScan scan, ArraySegment<T> value)
        {
            if (value.Array == null)
                scan.ScanEnumerable<T>(null, GetElementConverter(scan.Settings));
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
