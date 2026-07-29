using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    [BuffConverter(typeof(HashSet<>))]
    class HashSetConverter<T> : IEnumerableConverter<T, HashSet<T>>
    {
        protected override HashSet<T> OnRead(IBufferReader reader, Type type) => reader.ReadHashSet(ReadOnce);
    }
}
