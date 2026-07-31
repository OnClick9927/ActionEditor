using System.Collections.Generic;
using System.Text;

namespace ActionBuffer
{
    internal static class ClassPool
    {
        private static class Cache<T> where T : class
        {
            internal static readonly object SyncRoot = new object();
            internal static readonly Stack<T> Items = new Stack<T>();
        }

        internal static T Get<T>() where T : class, new()
        {
            lock (Cache<T>.SyncRoot)
            {
                return Cache<T>.Items.Count == 0 ? new T() : Cache<T>.Items.Pop();
            }
        }

        internal static void Back<T>(T value) where T : class
        {
            if (value == null) return;
            Trim(value);
            lock (Cache<T>.SyncRoot)
            {
                if (Cache<T>.Items.Count < BuffSettings.PoolLimit)
                    Cache<T>.Items.Push(value);
            }
        }

        internal static List<T> GetList<T>(int capacity = 0)
        {
            var result = Get<List<T>>();
            result.Clear();
            if (capacity > result.Capacity) result.Capacity = capacity;
            return result;
        }

        internal static void BackList<T>(List<T> value)
        {
            if (value == null) return;
            value.Clear();
            if (value.Capacity > BuffSettings.RetainedListCapacity)
                value.Capacity = 0;
            Back(value);
        }

        internal static HashSet<T> GetHashSet<T>()
        {
            var result = Get<HashSet<T>>();
            result.Clear();
            return result;
        }

        internal static void BackHashSet<T>(HashSet<T> value)
        {
            if (value == null) return;
            bool isLarge = value.Count > BuffSettings.RetainedListCapacity;
            value.Clear();
            if (isLarge) value.TrimExcess();
            Back(value);
        }

        private static void Trim<T>(T value) where T : class
        {
            if (value is StringBuilder builder)
            {
                if (builder.Capacity > BuffSettings.RetainedTextCapacity)
                {
                    builder.Clear();
                    builder.Capacity = 1024;
                }
                return;
            }
            if (value is BufferWriter binaryWriter)
            {
                binaryWriter.TrimCapacity();
                return;
            }
            if (value is JsonWriter jsonWriter)
            {
                jsonWriter.TrimCapacity();
                return;
            }
            if (value is YamlWriter yamlWriter)
            {
                yamlWriter.TrimCapacity();
                return;
            }
            if (value is XmlWriter xmlWriter)
                xmlWriter.TrimCapacity();
        }
    }
}
