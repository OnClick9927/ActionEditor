using System;

namespace ActionBuffer
{
    [BuffConverter(typeof(TimeSpan))]
    class TimeSpanConverter : AtomicBuffConverter<TimeSpan>
    {
        BuffConverter<long> converter = GetConverter<long>();
        protected override TimeSpan OnRead(IBufferReader reader, Type type)
        {
            return TimeSpan.FromTicks(converter.ReadValue(reader, typeof(long)));
        }

        protected override void OnWrite(IBufferWriter writer, TimeSpan value)
        {
            converter.WriteValue(writer, value.Ticks);
        }
    }
}
