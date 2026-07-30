using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class ReadOnlyCollectionInterfaceConverter<T> : IEnumerableConverter<T, IReadOnlyCollection<T>>
    {
        protected override IReadOnlyCollection<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadElement);
    }
}
