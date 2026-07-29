using System;
using System.Collections.Generic;
using System.Reflection;
namespace ActionBuffer
{
    [BuffConverter(typeof(KeyValuePair<,>))]
    class KeyValuePairConverter<Key, Value> : BuffConverter<KeyValuePair<Key, Value>>
    {
        static TypeHelper.TypeFields fields;
        static KeyValuePairConverter()
        {
            var _type = typeof(KeyValuePair<Key, Value>);
            fields = new TypeHelper.TypeFields(_type);
            fields.AddField(_type.GetField("key", BindingFlags.Instance | BindingFlags.NonPublic), true);
            fields.AddField(_type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic), true);
        }
        protected override KeyValuePair<Key, Value> OnRead(IBufferReader reader, Type type)
        {
            var instance = new KeyValuePair<Key, Value>(default, default);
            return reader.ReadObject<KeyValuePair<Key, Value>>(instance, fields);

        }

        protected override void OnScan(BufferScan scan, KeyValuePair<Key, Value> value)
        {
            scan.ScanObject(value, fields);
        }

        protected override void OnWrite(IBufferWriter writer, KeyValuePair<Key, Value> value)
        {
            writer.WriteObject(value, fields);
        }
    }
}
