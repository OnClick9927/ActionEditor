using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    class ClassPool<T> where T : class, new()
    {
        [ThreadStatic]
        private static Stack<T> _pool;

        public static T Get()
        {
            if (_pool != null && _pool.Count > 0)
                return _pool.Pop();
            return new T();
        }

        public static void Back(T value)
        {
            if (value == null) return;
            if (_pool == null)
                _pool = new Stack<T>();
            _pool.Push(value);
        }
    }
}
