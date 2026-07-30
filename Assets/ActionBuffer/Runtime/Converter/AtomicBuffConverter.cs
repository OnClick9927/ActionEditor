namespace ActionBuffer
{
    public abstract class AtomicBuffConverter<T> : BuffConverter<T>
    {
        protected override void OnScan(BufferScan scan, T value) { }
    }
}
