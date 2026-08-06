using System;

namespace ActionAttribute
{
    /// <summary>限制字符串字段可保存的最大字符数。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MaxLengthAttribute : ActionAttributeBase
    {
        public readonly int length;

        public MaxLengthAttribute(int length)
        {
            this.length = Math.Max(0, length);
        }
    }
}
