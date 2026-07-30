namespace ActionBuffer
{

    public interface IBufferObject
    {
        void BeforeWriteBuffer();
        void AfterReadBuffer();
    }
}

