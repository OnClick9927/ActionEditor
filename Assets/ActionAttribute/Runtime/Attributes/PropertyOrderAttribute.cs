using System;

namespace ActionAttribute
{
    /// <summary>指定成员在自定义检查器中的绘制顺序。</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method |
        AttributeTargets.Property)]
    public sealed class PropertyOrderAttribute : ActionAttributeBase
    {
        public readonly int value;

        public PropertyOrderAttribute(int order)
        {
            value = order;
        }
    }
}
