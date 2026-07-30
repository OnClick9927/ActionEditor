using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class QueueConverter<T> : IEnumerableConverter<T, Queue<T>>
    {
        protected override Queue<T> OnRead(IBufferReader reader, Type type) => reader.ReadQueue(ReadElement);
    }
}
