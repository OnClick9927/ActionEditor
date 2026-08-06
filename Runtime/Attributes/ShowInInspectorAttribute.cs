using System;

namespace ActionAttribute
{
    /// <summary>要求自定义检查器显示通常不会被 Unity 序列化绘制的成员。</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ShowInInspectorAttribute : ActionAttributeBase { }
}
