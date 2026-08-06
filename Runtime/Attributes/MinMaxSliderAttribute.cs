using System;

namespace ActionAttribute
{
    /// <summary>将双数值范围字段绘制为指定区间内的最小最大滑块。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinMaxSliderAttribute : ActionAttributeBase
    {
        public readonly float min;
        public readonly float max;

        public MinMaxSliderAttribute(float min, float max)
        {
            if (min >= max) throw new ArgumentException("min must be less than max");
            this.min = min;
            this.max = max;
        }
    }
}
