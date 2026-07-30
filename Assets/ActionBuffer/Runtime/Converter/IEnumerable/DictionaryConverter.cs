using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
namespace ActionBuffer
{
    internal sealed class KeyValueDeterministicComparer<Key, Value> : IComparer<KeyValuePair<Key, Value>>
    {
        internal static readonly KeyValueDeterministicComparer<Key, Value> Instance =
            new KeyValueDeterministicComparer<Key, Value>();

        public int Compare(KeyValuePair<Key, Value> left, KeyValuePair<Key, Value> right) =>
            DeterministicComparer<Key>.Instance.Compare(left.Key, right.Key);
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

    class DictionaryConverter<Key, Value> : IEnumerableConverter<KeyValuePair<Key, Value>, Dictionary<Key, Value>>
    {
        protected override void OnScan(BufferScan scan, Dictionary<Key, Value> value)
        {
            if (value != null) CollectionComparerGuard<Key>.RequireDefault(value.Comparer, value.GetType());
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<Key, Value>.Instance);
        }
        protected override Dictionary<Key, Value> OnRead(IBufferReader reader, Type type) => reader.ReadDictionary(ReadElement);
    }

    class DictionaryInterfaceConverter<Key, Value> :
        IEnumerableConverter<KeyValuePair<Key, Value>, IDictionary<Key, Value>>
    {
        protected override void OnScan(BufferScan scan, IDictionary<Key, Value> value)
        {
            if (value is Dictionary<Key, Value> dictionary)
                CollectionComparerGuard<Key>.RequireDefault(dictionary.Comparer, value.GetType());
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<Key, Value>.Instance);
        }
        protected override IDictionary<Key, Value> OnRead(IBufferReader reader, Type type) =>
            reader.ReadDictionary(ReadElement);
    }

    class ReadOnlyDictionaryInterfaceConverter<Key, Value> :
        IEnumerableConverter<KeyValuePair<Key, Value>, IReadOnlyDictionary<Key, Value>>
    {
        protected override void OnScan(BufferScan scan, IReadOnlyDictionary<Key, Value> value)
        {
            if (value is Dictionary<Key, Value> dictionary)
                CollectionComparerGuard<Key>.RequireDefault(dictionary.Comparer, value.GetType());
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<Key, Value>.Instance);
        }
        protected override IReadOnlyDictionary<Key, Value> OnRead(IBufferReader reader, Type type) =>
            reader.ReadDictionary(ReadElement);
    }

    class ConcurrentDictionaryConverter<Key, Value> :
        IEnumerableConverter<KeyValuePair<Key, Value>, ConcurrentDictionary<Key, Value>>
    {
        protected override void OnScan(BufferScan scan, ConcurrentDictionary<Key, Value> value)
        {
            ScanWithComparer(scan, value, KeyValueDeterministicComparer<Key, Value>.Instance);
        }

        protected override ConcurrentDictionary<Key, Value> OnRead(IBufferReader reader, Type type) =>
            reader.ReadConcurrentDictionary(ReadElement);
    }
}
