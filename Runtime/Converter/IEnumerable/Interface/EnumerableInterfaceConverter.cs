using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class EnumerableInterfaceConverter<T> : IEnumerableConverter<T, IEnumerable<T>>
    {
        protected override IEnumerable<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadElement);
    }
}
