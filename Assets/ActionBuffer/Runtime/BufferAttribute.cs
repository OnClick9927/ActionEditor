using System;


namespace ActionBuffer
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BufferAttribute : System.Attribute
    {
        public readonly string bufferName;
        public BufferAttribute() { }
        public BufferAttribute(string bufferName)
        {
            this.bufferName = bufferName;
        }
    }
}
