using System;

namespace ActionAttribute
{
    /// <summary>将字符串字段绘制为 Unity Tag 选择器。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TagAttribute : ActionAttributeBase { }
}
