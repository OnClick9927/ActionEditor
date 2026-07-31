using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace ActionBuffer
{
    public abstract class IEnumerableConverter<T, TCollection> : BuffConverter<TCollection>
        where TCollection : IEnumerable<T>
    {
        protected override void OnScan(BufferScan scan, TCollection value)
        {
            var converter = ConverterCache<T>.Get(scan);
            if (value is ISet<T>)
            {
                scan.ScanEnumerable(value, converter, scan.DeterministicCollectionOrder
                    ? DeterministicComparer<T>.Instance
                    : null);
                return;
            }
            if (value is IDictionary)
                throw new NotSupportedException(
                    $"Dictionary value '{value.GetType()}' must be declared as a supported dictionary interface.");
            scan.ScanEnumerable(value, converter);
        }

        protected void ScanWithComparer(BufferScan scan, TCollection value,
            IComparer<T> comparer) => scan.ScanEnumerable(value, ConverterCache<T>.Get(scan),
            scan.DeterministicCollectionOrder ? comparer : null);

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TCollection value) =>
            writer.WriteIEnumerable(scan, ConverterCache<T>.Get(scan));
    }

    internal sealed class ArrayConverter<T> : IEnumerableConverter<T, T[]>
    {
        protected override T[] OnRead(IBufferReader reader, Type type) =>
            reader.ReadArray(ConverterCache<T>.Get(reader));
    }

    internal sealed class ArraySegmentConverter<T> : IEnumerableConverter<T, ArraySegment<T>>
    {
        protected override void OnScan(BufferScan scan, ArraySegment<T> value)
        {
            if (value.Array == null)
                scan.ScanEnumerable<T>(null, ConverterCache<T>.Get(scan));
            else
                base.OnScan(scan, value);
        }

        protected override ArraySegment<T> OnRead(IBufferReader reader, Type type)
        {
            var values = reader.ReadArray(ConverterCache<T>.Get(reader));
            return values == null ? default : new ArraySegment<T>(values);
        }
    }

    internal sealed class MultiDimensionalArrayConverter<TArray, T> : BuffConverter<TArray>
        where TArray : class
    {
        private readonly int _rank = typeof(TArray).GetArrayRank();

        protected override void OnScan(BufferScan scan, TArray value) =>
            scan.ScanMultiDimensionalArray<T>(value as Array, _rank, ConverterCache<T>.Get(scan));
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TArray value) =>
            writer.WriteMultiDimensionalArray(scan, _rank, ConverterCache<T>.Get(scan));
        protected override TArray OnRead(IBufferReader reader, Type type) =>
            (TArray)(object)reader.ReadMultiDimensionalArray(_rank,
                ConverterCache<T>.Get(reader));
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

    internal sealed class ListConverter<T> : IEnumerableConverter<T, List<T>>
    {
        protected override List<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadList(ConverterCache<T>.Get(reader));
    }

    internal sealed class EnumerableInterfaceConverter<T> :
        IEnumerableConverter<T, IEnumerable<T>>
    {
        protected override IEnumerable<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadList(ConverterCache<T>.Get(reader));
    }

    internal sealed class CollectionInterfaceConverter<T> :
        IEnumerableConverter<T, ICollection<T>>
    {
        protected override ICollection<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadList(ConverterCache<T>.Get(reader));
    }

    internal sealed class ListInterfaceConverter<T> : IEnumerableConverter<T, IList<T>>
    {
        protected override IList<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadList(ConverterCache<T>.Get(reader));
    }

    internal sealed class ReadOnlyCollectionInterfaceConverter<T> :
        IEnumerableConverter<T, IReadOnlyCollection<T>>
    {
        protected override IReadOnlyCollection<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadList(ConverterCache<T>.Get(reader));
    }

    internal sealed class ReadOnlyListInterfaceConverter<T> :
        IEnumerableConverter<T, IReadOnlyList<T>>
    {
        protected override IReadOnlyList<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadList(ConverterCache<T>.Get(reader));
    }

    internal sealed class QueueConverter<T> : IEnumerableConverter<T, Queue<T>>
    {
        protected override Queue<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadQueue(ConverterCache<T>.Get(reader));
    }

    internal sealed class StackConverter<T> : IEnumerableConverter<T, Stack<T>>
    {
        protected override Stack<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadStack(ConverterCache<T>.Get(reader));
    }

    internal static class DeterministicComparer<T>
    {
        internal static readonly IComparer<T> Instance = Create();

        private static IComparer<T> Create()
        {
            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (type == typeof(string)) return (IComparer<T>)(object)StringComparer.Ordinal;
            if (type.IsEnum || typeof(IComparable).IsAssignableFrom(type) ||
                typeof(IComparable<>).MakeGenericType(type).IsAssignableFrom(type))
                return Comparer<T>.Default;
            return null;
        }
    }

    internal static class CollectionComparerGuard<T>
    {
        internal static void RequireDefault(IEqualityComparer<T> comparer, Type collectionType)
        {
            if (Equals(comparer, EqualityComparer<T>.Default)) return;
            throw new NotSupportedException(
                $"Collection type '{collectionType}' uses a custom comparer. " +
                "ActionBuffer serializes collection values only and cannot preserve comparer behavior.");
        }
    }

    internal sealed class HashSetConverter<T> : IEnumerableConverter<T, HashSet<T>>
    {
        protected override void OnScan(BufferScan scan, HashSet<T> value)
        {
            if (value != null) CollectionComparerGuard<T>.RequireDefault(value.Comparer, value.GetType());
            ScanWithComparer(scan, value, DeterministicComparer<T>.Instance);
        }

        protected override HashSet<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadHashSet(ConverterCache<T>.Get(reader));
    }

    internal sealed class SetInterfaceConverter<T> : IEnumerableConverter<T, ISet<T>>
    {
        protected override void OnScan(BufferScan scan, ISet<T> value)
        {
            if (value is HashSet<T> set)
                CollectionComparerGuard<T>.RequireDefault(set.Comparer, value.GetType());
            ScanWithComparer(scan, value, DeterministicComparer<T>.Instance);
        }

        protected override ISet<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadHashSet(ConverterCache<T>.Get(reader));
    }

    internal sealed class KeyValueDeterministicComparer<TKey, TValue> :
        IComparer<KeyValuePair<TKey, TValue>>
    {
        internal static readonly IComparer<KeyValuePair<TKey, TValue>> Instance =
            DeterministicComparer<TKey>.Instance == null
                ? null
                : new KeyValueDeterministicComparer<TKey, TValue>();

        public int Compare(KeyValuePair<TKey, TValue> left,
            KeyValuePair<TKey, TValue> right) =>
            DeterministicComparer<TKey>.Instance.Compare(left.Key, right.Key);
    }

    internal sealed class DictionaryConverter<TKey, TValue> :
        IEnumerableConverter<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>>
    {
        protected override void OnScan(BufferScan scan, Dictionary<TKey, TValue> value)
        {
            if (value != null)
                CollectionComparerGuard<TKey>.RequireDefault(value.Comparer, value.GetType());
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<TKey, TValue>.Instance);
        }

        protected override Dictionary<TKey, TValue> OnRead(IBufferReader reader, Type type) =>
            reader.ReadDictionary(ConverterCache<KeyValuePair<TKey, TValue>>.Get(reader));
    }

    internal sealed class DictionaryInterfaceConverter<TKey, TValue> :
        IEnumerableConverter<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>>
    {
        protected override void OnScan(BufferScan scan, IDictionary<TKey, TValue> value)
        {
            if (value is Dictionary<TKey, TValue> dictionary)
                CollectionComparerGuard<TKey>.RequireDefault(dictionary.Comparer, value.GetType());
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<TKey, TValue>.Instance);
        }

        protected override IDictionary<TKey, TValue> OnRead(IBufferReader reader, Type type) =>
            reader.ReadDictionary(ConverterCache<KeyValuePair<TKey, TValue>>.Get(reader));
    }

    internal sealed class ReadOnlyDictionaryInterfaceConverter<TKey, TValue> :
        IEnumerableConverter<KeyValuePair<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>>
    {
        protected override void OnScan(BufferScan scan, IReadOnlyDictionary<TKey, TValue> value)
        {
            if (value is Dictionary<TKey, TValue> dictionary)
                CollectionComparerGuard<TKey>.RequireDefault(dictionary.Comparer, value.GetType());
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<TKey, TValue>.Instance);
        }

        protected override IReadOnlyDictionary<TKey, TValue> OnRead(IBufferReader reader, Type type) =>
            reader.ReadDictionary(ConverterCache<KeyValuePair<TKey, TValue>>.Get(reader));
    }

    internal sealed class ConcurrentDictionaryConverter<TKey, TValue> :
        IEnumerableConverter<KeyValuePair<TKey, TValue>, ConcurrentDictionary<TKey, TValue>>
    {
        protected override void OnScan(BufferScan scan, ConcurrentDictionary<TKey, TValue> value)
        {
            if (value != null)
                CollectionComparerGuard<TKey>.RequireDefault(
                    ConcurrentDictionaryComparer<TKey, TValue>.Get(value), value.GetType());
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<TKey, TValue>.Instance);
        }

        protected override ConcurrentDictionary<TKey, TValue> OnRead(IBufferReader reader,
            Type type) => reader.ReadConcurrentDictionary(
                ConverterCache<KeyValuePair<TKey, TValue>>.Get(reader));
    }

    internal static class ConcurrentDictionaryComparer<TKey, TValue>
    {
        private static readonly PropertyInfo ComparerProperty =
            typeof(ConcurrentDictionary<TKey, TValue>).GetProperty("Comparer",
                BindingFlags.Public | BindingFlags.Instance);
        private static readonly FieldInfo ComparerField = FindComparerField(
            typeof(ConcurrentDictionary<TKey, TValue>));
        private static readonly FieldInfo TablesField =
            typeof(ConcurrentDictionary<TKey, TValue>).GetField("_tables",
                BindingFlags.NonPublic | BindingFlags.Instance) ??
            typeof(ConcurrentDictionary<TKey, TValue>).GetField("m_tables",
                BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo TablesComparerField = TablesField == null
            ? null
            : FindComparerField(TablesField.FieldType);

        internal static IEqualityComparer<TKey> Get(
            ConcurrentDictionary<TKey, TValue> dictionary)
        {
            object comparer = ComparerProperty?.GetValue(dictionary, null) ??
                              ComparerField?.GetValue(dictionary);
            if (comparer == null && TablesField != null && TablesComparerField != null)
            {
                var tables = TablesField.GetValue(dictionary);
                if (tables != null) comparer = TablesComparerField.GetValue(tables);
            }
            if (comparer is IEqualityComparer<TKey> typed) return typed;
            throw new NotSupportedException(
                $"Cannot inspect the comparer used by collection type '{dictionary.GetType()}'.");
        }

        private static FieldInfo FindComparerField(Type type)
        {
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
                if (typeof(IEqualityComparer<TKey>).IsAssignableFrom(fields[i].FieldType))
                    return fields[i];
            return null;
        }
    }
}
