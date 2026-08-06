using System;

namespace ActionAttribute
{
    /// <summary>将整数或字符串字段绘制为 Unity Layer 选择器。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LayerAttribute : ActionAttributeBase { }
}
