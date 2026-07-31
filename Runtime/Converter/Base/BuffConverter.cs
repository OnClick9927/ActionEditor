using System;

namespace ActionBuffer
{
    public abstract class BuffConverter
    {
        internal abstract object Read(IBufferReader reader, Type type);
        internal abstract void Scan(BufferScan scan, object value);
        internal abstract void Write(IBufferWriter writer, BufferScan scan, object value);
        internal virtual bool UsesObjectLayout => false;
    }

    public abstract class BuffConverter<T> : BuffConverter
    {
        protected abstract void OnScan(BufferScan scan, T value);
        protected abstract void OnWrite(IBufferWriter writer, BufferScan scan, T value);
        protected abstract T OnRead(IBufferReader reader, Type type);

        internal T ReadValue(IBufferReader reader, Type type) => OnRead(reader, type);

        internal void ScanValue(BufferScan scan, T value)
        {
            scan.CountNode();
            OnScan(scan, value);
        }

        internal void WriteValue(IBufferWriter writer, BufferScan scan, T value) =>
            OnWrite(writer, scan, value);

        internal sealed override object Read(IBufferReader reader, Type type) => ReadValue(reader, type);
        internal sealed override void Scan(BufferScan scan, object value) => ScanValue(scan, (T)value);
        internal sealed override void Write(IBufferWriter writer, BufferScan scan, object value) =>
            WriteValue(writer, scan, (T)value);
    }
}
