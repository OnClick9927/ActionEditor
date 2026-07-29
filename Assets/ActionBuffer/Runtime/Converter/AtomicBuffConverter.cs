namespace ActionBuffer
{
    public abstract class AtomicBuffConverter<T> : BuffConverter<T>
    {
        protected sealed override void OnScan(BufferScan scan, T value) { }
    }
}
