using System;
using System.Collections.Generic;


namespace ActionBuffer
{
    public interface IBufferReader
    {
        List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read);
        List<T> ReadList<T>(Func<IBufferReader, T> read)
        {
            var result = ClassPool<List<T>>.Get();
            result.Clear();
            try
            {
                var values = ReadIEnumerable(result, read);
                if (values != null) return values;
                result.Clear();
                ClassPool<List<T>>.Back(result);
                return null;
            }
            catch
            {
                result.Clear();
                ClassPool<List<T>>.Back(result);
                throw;
            }
        }

        T[] ReadArray<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : values.ToArray();
            }
            finally
            {
                list.Clear();
                ClassPool<List<T>>.Back(list);
            }
        }
        HashSet<T> ReadHashSet<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : new HashSet<T>(values);
            }
            finally
            {
                list.Clear();
                ClassPool<List<T>>.Back(list);
            }
        }
        Stack<T> ReadStack<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
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
                ClassPool<List<T>>.Back(list);
            }
        }
        Queue<T> ReadQueue<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : new Queue<T>(values);
            }
            finally
            {
                list.Clear();
                ClassPool<List<T>>.Back(list);
            }
        }
        Dictionary<Key, Value> ReadDictionary<Key, Value>(Func<IBufferReader, KeyValuePair<Key, Value>> read)
        {
            var list = ClassPool<List<KeyValuePair<Key, Value>>>.Get();
            list.Clear();
            try
            {
                var values = ReadIEnumerable(list, read);
                return values == null ? null : new Dictionary<Key, Value>(values);
            }
            finally
            {
                list.Clear();
                ClassPool<List<KeyValuePair<Key, Value>>>.Back(list);
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
        T ReadObject<T>(object instance, TypeHelper.TypeFields fields);

        ushort ReadUInt16();
        uint ReadUInt32();
        ulong ReadUInt64();
        string ReadUTF8();
    }
}
