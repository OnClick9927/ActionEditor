using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    [BuffConverter(typeof(List<>))]
    class ListConverter<T> : IEnumerableConverter<T, List<T>>
    {
        protected override List<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadOnce);
    }
}
