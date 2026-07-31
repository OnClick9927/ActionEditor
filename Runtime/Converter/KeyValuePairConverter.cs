using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    internal sealed class KeyValuePairConverter<TKey, TValue> :
        BuffConverter<KeyValuePair<TKey, TValue>>
    {
        protected override KeyValuePair<TKey, TValue> OnRead(IBufferReader reader, Type type) =>
            reader.ReadKeyValuePair(ConverterCache<TKey>.Get(reader),
                ConverterCache<TValue>.Get(reader));

        protected override void OnScan(BufferScan scan, KeyValuePair<TKey, TValue> value)
        {
            ConverterCache<TKey>.Get(scan).ScanValue(scan, value.Key);
            ConverterCache<TValue>.Get(scan).ScanValue(scan, value.Value);
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            KeyValuePair<TKey, TValue> value) =>
            writer.WriteKeyValuePair(scan, value, ConverterCache<TKey>.Get(scan),
                ConverterCache<TValue>.Get(scan));
    }
}
