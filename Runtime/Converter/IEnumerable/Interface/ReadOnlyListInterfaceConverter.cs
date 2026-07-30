using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class ReadOnlyListInterfaceConverter<T> : IEnumerableConverter<T, IReadOnlyList<T>>
    {
        protected override IReadOnlyList<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadElement);
    }
}
