using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class CollectionInterfaceConverter<T> : IEnumerableConverter<T, ICollection<T>>
    {
        protected override ICollection<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadElement);
    }
}
