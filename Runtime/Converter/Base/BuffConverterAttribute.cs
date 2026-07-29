using System;


namespace ActionBuffer
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BuffConverterAttribute : System.Attribute
    {
        public Type type;

        public BuffConverterAttribute(Type type)
        {
            this.type = type;
        }
    }
}
