using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    [BuffConverter(typeof(Queue<>))]
    class QueueConverter<T> : IEnumerableConverter<T, Queue<T>>
    {
        protected override Queue<T> OnRead(IBufferReader reader, Type type) => reader.ReadQueue(ReadOnce);
    }
}
