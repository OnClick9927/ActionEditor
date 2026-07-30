using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class StackConverter<T> : IEnumerableConverter<T, Stack<T>>
    {
        protected override Stack<T> OnRead(IBufferReader reader, Type type) => reader.ReadStack(ReadElement);
    }
}
