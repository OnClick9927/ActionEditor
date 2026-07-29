using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(bool))]
    class BoolConverter : AtomicBuffConverter<bool>
    {

        protected override bool OnRead(IBufferReader reader, Type type) => reader.ReadBool();
        protected override void OnWrite(IBufferWriter writer, bool value) => writer.WriteBool(value);
    }
}
