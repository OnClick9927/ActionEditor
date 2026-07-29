using System;
namespace ActionBuffer
{
    class ArrayConverter<T> : IEnumerableConverter<T, T[]>
    {
        protected override T[] OnRead(IBufferReader reader, Type type) => reader.ReadArray(ReadOnce);
    }
}
