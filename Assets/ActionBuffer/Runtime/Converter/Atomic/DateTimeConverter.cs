using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(DateTime))]
    class DateTimeConverter : AtomicBuffConverter<DateTime>
    {
        BuffConverter<long> converter = GetConverter<long>();
        protected override DateTime OnRead(IBufferReader reader, Type type)
        {
            return new DateTime(converter.ReadValue(reader, typeof(long)));
        }

        protected override void OnWrite(IBufferWriter writer, DateTime value)
        {
            converter.WriteValue(writer, value.Ticks);
        }
    }
}
