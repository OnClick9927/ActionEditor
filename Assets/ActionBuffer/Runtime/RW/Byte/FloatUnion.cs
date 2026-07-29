using System.Runtime.InteropServices;


namespace ActionBuffer
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    struct FloatUnion
    {
        [FieldOffset(0)]
        public float value;
        [FieldOffset(0)]

        public int _int;
    }
}
