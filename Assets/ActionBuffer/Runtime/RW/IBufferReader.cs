using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ActionBuffer
{
    public interface IBufferReader
    {
        List<T> ReadIEnumerable<T>(List<T> result, BuffConverter<T> converter);
        Array ReadMultiDimensionalArray<T>(int rank, BuffConverter<T> converter);
        T? ReadNullable<T>(BuffConverter<T> converter) where T : struct;
        KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>(
            BuffConverter<TKey> keyConverter, BuffConverter<TValue> valueConverter);
        List<T> ReadList<T>(BuffConverter<T> converter);
        T[] ReadArray<T>(BuffConverter<T> converter);
        HashSet<T> ReadHashSet<T>(BuffConverter<T> converter);
        Stack<T> ReadStack<T>(BuffConverter<T> converter);
        Queue<T> ReadQueue<T>(BuffConverter<T> converter);
        Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BuffConverter<KeyValuePair<TKey, TValue>> converter);
        ConcurrentDictionary<TKey, TValue> ReadConcurrentDictionary<TKey, TValue>(
            BuffConverter<KeyValuePair<TKey, TValue>> converter);
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
        Guid ReadGuid();
    }

    internal interface IObjectContextReader
    {
        object CurrentObject { get; }
        object GetOrCreateReference(int referenceId, Type type);
    }

    internal interface IBuffSerializerContext
    {
        BuffSettings Settings { get; }
    }

    internal interface ITypedEnumReader
    {
        T ReadEnumValue<T>() where T : struct, Enum;
    }

    public interface IReferenceResolver
    {
        void EnsureReferencesResolved();
    }
}
