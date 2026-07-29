using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;


namespace ActionBuffer
{

    public interface IBufferObject
    {
        void BeforeWriteBuffer();
        void AfterReadBuffer();
    }
}

