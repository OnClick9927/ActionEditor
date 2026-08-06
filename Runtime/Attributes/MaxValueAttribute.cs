using System;

namespace ActionAttribute
{
    /// <summary>限制数值字段不得大于指定值。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MaxValueAttribute : ActionAttributeBase
    {
        public readonly double value;

        public MaxValueAttribute(double value)
        {
            this.value = value;
        }
    }
}
