using System;

namespace ActionAttribute
{
    /// <summary>将数值字段循环约束在指定区间内，越界后从另一端继续。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class WrapAttribute : ActionAttributeBase
    {
        public readonly double min;
        public readonly double max;

        public WrapAttribute(double min, double max)
        {
            if (min >= max) throw new ArgumentException("min must be less than max");
            this.min = min;
            this.max = max;
        }
    }
}
