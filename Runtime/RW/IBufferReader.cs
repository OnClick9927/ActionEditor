using System;
using System.Collections.Generic;
using System.Linq;


namespace ActionBuffer
{
    public interface IBufferReader
    {
        List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read);
        List<T> ReadList<T>(Func<IBufferReader, T> read)
        {
            var result = ClassPool<List<T>>.Get();
            result.Clear();
            return ReadIEnumerable(result, read);
        }

        T[] ReadArray<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
            list = ReadIEnumerable(list, read);
            var result = list.ToArray();
            list.Clear();
            ClassPool<List<T>>.Back(list);
            return result;
        }
        HashSet<T> ReadHashSet<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
            list = ReadIEnumerable(list, read);
            var result = list.ToHashSet();
            list.Clear();
            ClassPool<List<T>>.Back(list);
            return result;
        }
        Stack<T> ReadStack<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
            list = ReadIEnumerable(list, read);
            Stack<T> result = new Stack<T>(list);
            list.Clear();
            ClassPool<List<T>>.Back(list);
            return result;
        }
        Queue<T> ReadQueue<T>(Func<IBufferReader, T> read)
        {
            var list = ClassPool<List<T>>.Get();
            list.Clear();
            list = ReadIEnumerable(list, read);
            Queue<T> result = new Queue<T>(list);
            list.Clear();
            ClassPool<List<T>>.Back(list);
            return result;
        }
        Dictionary<Key, Value> ReadDictionary<Key, Value>(Func<IBufferReader, KeyValuePair<Key, Value>> read)
        {
            var list = ClassPool<List<KeyValuePair<Key, Value>>>.Get();
            list.Clear();
            list = ReadIEnumerable(list, read);
            var result = new Dictionary<Key, Value>(list);
            list.Clear();
            ClassPool<List<KeyValuePair<Key, Value>>>.Back(list);
            return result;
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
