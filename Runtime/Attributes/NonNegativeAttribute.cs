using System;

namespace ActionAttribute
{
    /// <summary>将整数或浮点字段限制为大于等于零。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NonNegativeAttribute : ActionAttributeBase { }
}
