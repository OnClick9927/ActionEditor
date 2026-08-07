using System;

namespace ActionAttribute
{
    /// <summary>将整数或浮点字段限制为严格大于零。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PositiveAttribute : ActionAttributeBase { }
}
