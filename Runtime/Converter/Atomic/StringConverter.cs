using System;

namespace ActionBuffer
{
    class StringConverter : AtomicBuffConverter<string>
    {
        protected override void OnScan(BufferScan scan, string value)
        {
            if (value != null && value.Length > BufferSerializer.MaxScalarLength)
                throw new FormatException(
                    $"String length cannot exceed {BufferSerializer.MaxScalarLength} characters.");
        }

        protected override string OnRead(IBufferReader reader, Type type) => reader.ReadUTF8();
        protected override void OnWrite(IBufferWriter writer, BufferScan scan, string value) => writer.WriteUTF8(value);
    }
}
