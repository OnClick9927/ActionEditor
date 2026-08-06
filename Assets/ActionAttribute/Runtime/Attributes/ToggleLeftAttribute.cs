using System;

namespace ActionAttribute
{
    /// <summary>将布尔字段绘制为标签位于右侧的复选框。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ToggleLeftAttribute : ActionAttributeBase { }
}
