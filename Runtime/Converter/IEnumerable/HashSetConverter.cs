using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    internal static class DeterministicComparer<T>
    {
        internal static readonly IComparer<T> Instance = typeof(T) == typeof(string)
            ? (IComparer<T>)(object)StringComparer.Ordinal
            : Comparer<T>.Default;
    }

    class HashSetConverter<T> : IEnumerableConverter<T, HashSet<T>>
    {
        protected override void OnScan(BufferScan scan, HashSet<T> value)
        {
            if (value != null) CollectionComparerGuard<T>.RequireDefault(value.Comparer, value.GetType());
            ScanWithComparer(scan, value, DeterministicComparer<T>.Instance);
        }
        protected override HashSet<T> OnRead(IBufferReader reader, Type type) => reader.ReadHashSet(ReadElement);
    }

    class SetInterfaceConverter<T> : IEnumerableConverter<T, ISet<T>>
    {
        protected override void OnScan(BufferScan scan, ISet<T> value)
        {
            if (value is HashSet<T> hashSet)
                CollectionComparerGuard<T>.RequireDefault(hashSet.Comparer, value.GetType());
            ScanWithComparer(scan, value, DeterministicComparer<T>.Instance);
        }
        protected override ISet<T> OnRead(IBufferReader reader, Type type) => reader.ReadHashSet(ReadElement);
    }
}
