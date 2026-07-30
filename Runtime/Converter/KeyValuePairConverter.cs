using System;
using System.Collections.Generic;
namespace ActionBuffer
{
    class KeyValuePairConverter<Key, Value> : BuffConverter<KeyValuePair<Key, Value>>
    {
        private BuffConverter<Key> _keyConverter;
        private BuffConverter<Value> _valueConverter;
        private int _converterVersion = -1;
        private readonly Func<IBufferReader, Key> _readKey;
        private readonly Func<IBufferReader, Value> _readValue;
        private readonly Action<IBufferWriter, BufferScan, Key> _writeKey;
        private readonly Action<IBufferWriter, BufferScan, Value> _writeValue;

        public KeyValuePairConverter()
        {
            _readKey = ReadKeyValue;
            _readValue = ReadValueValue;
            _writeKey = WriteKeyValue;
            _writeValue = WriteValueValue;
        }

        private void EnsureConverters()
        {
            if (_converterVersion == BufferSerializer.ConverterVersion) return;
            _keyConverter = BufferSerializer.GetConverter<Key>();
            _valueConverter = BufferSerializer.GetConverter<Value>();
            _converterVersion = BufferSerializer.ConverterVersion;
        }
        protected override KeyValuePair<Key, Value> OnRead(IBufferReader reader, Type type) =>
            reader.ReadKeyValuePair(_readKey, _readValue);

        protected override void OnScan(BufferScan scan, KeyValuePair<Key, Value> value)
        {
            EnsureConverters();
            _keyConverter.ScanValue(scan, value.Key);
            _valueConverter.ScanValue(scan, value.Value);
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, KeyValuePair<Key, Value> value)
        {
            writer.WriteKeyValuePair(scan, value, _writeKey, _writeValue);
        }

        private Key ReadKeyValue(IBufferReader reader)
        {
            EnsureConverters();
            return _keyConverter.ReadValue(reader, typeof(Key));
        }
        private Value ReadValueValue(IBufferReader reader)
        {
            EnsureConverters();
            return _valueConverter.ReadValue(reader, typeof(Value));
        }
        private void WriteKeyValue(IBufferWriter writer, BufferScan scan, Key value)
        {
            EnsureConverters();
            _keyConverter.WriteValue(writer, scan, value);
        }
        private void WriteValueValue(IBufferWriter writer, BufferScan scan, Value value)
        {
            EnsureConverters();
            _valueConverter.WriteValue(writer, scan, value);
        }
    }
}
