using System;

namespace ActionAttribute
{
    /// <summary>标记检查器首次初始化目标时调用的无参方法。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorInitAttribute : ActionAttributeBase { }
}
