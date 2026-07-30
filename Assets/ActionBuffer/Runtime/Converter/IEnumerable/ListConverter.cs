using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class ListConverter<T> : IEnumerableConverter<T, List<T>>
    {
        protected override List<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadElement);
    }
}
