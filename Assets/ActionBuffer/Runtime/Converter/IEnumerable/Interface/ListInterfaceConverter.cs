using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class ListInterfaceConverter<T> : IEnumerableConverter<T, IList<T>>
    {
        protected override IList<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadElement);
    }
}
