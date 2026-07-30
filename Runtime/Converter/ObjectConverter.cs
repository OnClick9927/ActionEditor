using System;

namespace ActionBuffer
{
    class ObjectConverter<T> : BuffConverter<T>
    {
        internal override bool UsesObjectLayout => true;
        protected override void OnScan(BufferScan scan, T value) => scan.ScanObject(value);
        protected override T OnRead(IBufferReader reader, Type type) => reader.ReadObject<T>();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T value) => writer.WriteObject(scan, value);

    }
}
