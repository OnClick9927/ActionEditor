using System;

namespace ActionAttribute
{
    /// <summary>校验数组或列表元素数量是否位于指定范围内。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredListLengthAttribute : ActionAttributeBase
    {
        public readonly int min;
        public readonly int max;

        public RequiredListLengthAttribute(int min, int max = int.MaxValue)
        {
            if (min < 0 || max < min)
                throw new ArgumentException("Invalid required list length");
            this.min = min;
            this.max = max;
        }
    }
}
