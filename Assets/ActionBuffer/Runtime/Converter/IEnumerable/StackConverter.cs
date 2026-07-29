using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    [BuffConverter(typeof(Stack<>))]
    class StackConverter<T> : IEnumerableConverter<T, Stack<T>>
    {
        protected override Stack<T> OnRead(IBufferReader reader, Type type) => reader.ReadStack(ReadOnce);
    }
}
