using System;

namespace ActionAttribute
{
    /// <summary>限制数值字段不得小于指定值。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinValueAttribute : ActionAttributeBase
    {
        public readonly double value;

        public MinValueAttribute(double value)
        {
            this.value = value;
        }
    }
}
