using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    [BuffConverter(typeof(Dictionary<,>))]
    class DictionaryConverter<Key, Value> : IEnumerableConverter<KeyValuePair<Key, Value>, Dictionary<Key, Value>>
    {
        protected override Dictionary<Key, Value> OnRead(IBufferReader reader, Type type) => reader.ReadDictionary(ReadOnce);
    }
}
