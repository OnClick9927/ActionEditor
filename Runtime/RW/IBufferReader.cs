using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace ActionBuffer
{
    public interface IBufferReader
    {
        List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read);
        Array ReadMultiDimensionalArray<T>(int rank, Func<IBufferReader, T> read) =>
            throw new NotSupportedException("This reader does not support multi-dimensional arrays.");
        T? ReadNullable<T>(Func<IBufferReader, T> read) where T : struct;
        KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>(
            Func<IBufferReader, TKey> readKey, Func<IBufferReader, TValue> readValue) =>
            throw new NotSupportedException("This reader does not support KeyValuePair values.");
        List<T> ReadList<T>(Func<IBufferReader, T> read)
        {
            var result = ListPool<T>.Get();
            try
            {
                var values = ReadIEnumerable(result, read);
                if (values != null) return values;
                result.Clear();
                ListPool<T>.Back(result);
                return null;
            }
            catch
            {
                result.Clear();
                ListPool<T>.Back(result);
                throw;
            }
        }

        T[] ReadArray<T>(Func<IBufferReader, T> read)
        {
            var list = ListPool<T>.Get();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : values.ToArray();
            }
            finally
            {
                list.Clear();
                ListPool<T>.Back(list);
            }
        }
        HashSet<T> ReadHashSet<T>(Func<IBufferReader, T> read)
        {
            var list = ListPool<T>.Get();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : new HashSet<T>(values);
            }
            finally
            {
                list.Clear();
                ListPool<T>.Back(list);
            }
        }
        Stack<T> ReadStack<T>(Func<IBufferReader, T> read)
        {
            var list = ListPool<T>.Get();
            try
            {
                var values = ReadIEnumerable(list, read);
                if (values == null) return null;
                var result = new Stack<T>(values.Count);
                for (int i = values.Count - 1; i >= 0; i--)
                    result.Push(values[i]);
                return result;
            }
            finally
            {
                list.Clear();
                ListPool<T>.Back(list);
            }
        }
        Queue<T> ReadQueue<T>(Func<IBufferReader, T> read)
        {
            var list = ListPool<T>.Get();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : new Queue<T>(values);
            }
            finally
            {
                list.Clear();
                ListPool<T>.Back(list);
            }
        }
        Dictionary<Key, Value> ReadDictionary<Key, Value>(Func<IBufferReader, KeyValuePair<Key, Value>> read)
        {
            var list = ListPool<KeyValuePair<Key, Value>>.Get();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : new Dictionary<Key, Value>(values);
            }
            finally
            {
                list.Clear();
                ListPool<KeyValuePair<Key, Value>>.Back(list);
            }
        }
        ConcurrentDictionary<Key, Value> ReadConcurrentDictionary<Key, Value>(
            Func<IBufferReader, KeyValuePair<Key, Value>> read)
        {
            var list = ListPool<KeyValuePair<Key, Value>>.Get();
            try
            {
                var values = ReadIEnumerable(list, read);
                if (values == null) return null;
                var result = new ConcurrentDictionary<Key, Value>();
                for (int i = 0; i < values.Count; i++)
                {
                    var item = values[i];
                    if (!result.TryAdd(item.Key, item.Value))
                        throw new FormatException($"Duplicate dictionary key '{item.Key}'.");
                }
                return result;
            }
            finally
            {
                list.Clear();
                ListPool<KeyValuePair<Key, Value>>.Back(list);
            }
        }
        bool ReadBool();
        byte ReadByte();
        char ReadChar();
        double ReadDouble();
        Enum ReadEnum(Type type);
        float ReadFloat();
        short ReadInt16();
        int ReadInt32();
        long ReadInt64();
        T ReadObject<T>();

        ushort ReadUInt16();
        uint ReadUInt32();
        ulong ReadUInt64();
        string ReadUTF8();
        Guid ReadGuid() => Guid.ParseExact(ReadUTF8(), "D");
    }

    internal interface IObjectContextReader
    {
        object CurrentObject { get; }
        object GetOrCreateReference(int referenceId, Type type);
    }
}
