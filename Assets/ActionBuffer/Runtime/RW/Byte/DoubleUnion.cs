using System.Runtime.InteropServices;


namespace ActionBuffer
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    struct DoubleUnion
    {
        [FieldOffset(0)]
        public double value;
        [FieldOffset(0)]

        public long _long;
    }
}
