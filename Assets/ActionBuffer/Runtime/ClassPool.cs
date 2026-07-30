using System.Collections.Generic;
using System.Text;

namespace ActionBuffer
{
    class ClassPool<T> where T : class, new()
    {
        private static Stack<T> _pool;

        public static T Get()
        {
            if (_pool != null && _pool.Count > 0)
                return _pool.Pop();
            return new T();
        }

        public static void Back(T value)
        {
            if (value == null) return;
            if (value is StringBuilder builder && builder.Capacity > BufferSerializer.RetainedTextCapacity)
                builder.Capacity = 1024;
            if (_pool == null)
                _pool = new Stack<T>();
            if (_pool.Count >= BufferSerializer.PoolLimit) return;
            _pool.Push(value);
        }
    }

    internal static class ListPool<T>
    {
        private static Stack<List<T>> _pool;

        internal static List<T> Get(int capacity = 0)
        {
            var result = _pool != null && _pool.Count > 0 ? _pool.Pop() : new List<T>(capacity);
            result.Clear();
            if (capacity > result.Capacity) result.Capacity = capacity;
            return result;
        }

        internal static void Back(List<T> value)
        {
            if (value == null) return;
            value.Clear();
            if (value.Capacity > BufferSerializer.RetainedListCapacity) return;
            if (_pool == null) _pool = new Stack<List<T>>();
            if (_pool.Count >= BufferSerializer.PoolLimit) return;
            _pool.Push(value);
        }
    }

    internal static class HashSetPool<T>
    {
        private static Stack<HashSet<T>> _pool;

        internal static HashSet<T> Get()
        {
            var result = _pool != null && _pool.Count > 0 ? _pool.Pop() : new HashSet<T>();
            result.Clear();
            return result;
        }

        internal static void Back(HashSet<T> value)
        {
            if (value == null) return;
            int count = value.Count;
            value.Clear();
            if (count > BufferSerializer.RetainedListCapacity) return;
            if (_pool == null) _pool = new Stack<HashSet<T>>();
            if (_pool.Count >= BufferSerializer.PoolLimit) return;
            _pool.Push(value);
        }
    }
}
