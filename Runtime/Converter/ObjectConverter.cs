using System;

namespace ActionBuffer
{
    internal sealed class ObjectConverter<T> : BuffConverter<T>
    {
        internal const string PolymorphicValueField = "$ActionBuffer.Value.v1";
        internal override bool UsesObjectLayout => true;
        protected override void OnScan(BufferScan scan, T value)
        {
            object boxed = value;
            if (boxed != null && boxed.GetType() != typeof(T))
            {
                var converter = ConverterResolver.Get(boxed.GetType(), scan.Settings);
                if (!converter.UsesObjectLayout)
                {
                    scan.ScanPolymorphic(boxed, typeof(T), converter);
                    return;
                }
            }
            scan.ScanObject(value);
        }

        protected override T OnRead(IBufferReader reader, Type type)
        {
            if (reader is IPolymorphicReader polymorphic &&
                polymorphic.TryReadPolymorphic(typeof(T), out var value))
                return (T)value;
            return reader.ReadObject<T>();
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, T value)
        {
            if (value != null && value.GetType() != typeof(T) &&
                !ConverterResolver.Get(value.GetType(), scan.Settings).UsesObjectLayout)
            {
                WritePolymorphic(writer, scan);
                return;
            }
            writer.WriteObject(scan, value,
                value == null ? null : TypeHelper.GetTypeFields(value.GetType()));
        }

        private static void WritePolymorphic(IBufferWriter writer, BufferScan scan)
        {
            if (writer is IPolymorphicWriter polymorphic)
            {
                polymorphic.WritePolymorphic(scan);
                return;
            }
            throw new NotSupportedException(
                $"Writer '{writer.GetType()}' does not support polymorphic non-object values.");
        }
    }
}
