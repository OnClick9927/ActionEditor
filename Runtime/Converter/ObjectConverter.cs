using System;

namespace ActionBuffer
{
    class ObjectConverter<T> : BuffConverter<T>
    {
        protected override void OnScan(BufferScan scan, T value) => scan.ScanObject(value);
        protected override T OnRead(IBufferReader reader, Type type) => reader.ReadObject<T>();
        protected override void OnWrite(IBufferWriter writer, T value) => writer.WriteObject(value);

    }
}
