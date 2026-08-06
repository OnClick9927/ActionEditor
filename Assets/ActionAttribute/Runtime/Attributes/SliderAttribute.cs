using System;

namespace ActionAttribute
{
    /// <summary>将整数或浮点数字段绘制为带范围限制的滑杆。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SliderAttribute : ActionAttributeBase
    {
        public readonly double min;
        public readonly double max;

        public SliderAttribute(double min, double max)
        {
            if (min >= max)
                throw new ArgumentException("min must be less than max");
            this.min = min;
            this.max = max;
        }
    }
}
