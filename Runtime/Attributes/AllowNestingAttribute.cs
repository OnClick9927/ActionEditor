using System;

namespace ActionAttribute
{
    /// <summary>允许该特性在嵌套对象或集合元素的检查器绘制中继续生效。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AllowNestingAttribute : ActionAttributeBase { }
}
