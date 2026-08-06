using System;

namespace ActionAttribute
{
    /// <summary>将数值字段限制在指定的最小值和最大值之间。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ClampAttribute : ActionAttributeBase
    {
        public readonly double min;
        public readonly double max;

        public ClampAttribute(double min, double max)
        {
            if (min > max)
                throw new ArgumentException("min cannot be greater than max");
            this.min = min;
            this.max = max;
        }
    }
}
