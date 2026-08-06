using System;

namespace ActionAttribute
{
    /// <summary>将数值字段按指定范围绘制为进度条。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ProgressBarAttribute : ActionAttributeBase
    {
        public readonly double min;
        public readonly double max;
        public readonly string label;

        public ProgressBarAttribute(double max, string label = null)
            : this(0, max, label)
        {
        }

        public ProgressBarAttribute(double min, double max, string label = null)
        {
            if (min >= max)
                throw new ArgumentException("min must be less than max");
            this.min = min;
            this.max = max;
            this.label = label;
        }
    }
}
