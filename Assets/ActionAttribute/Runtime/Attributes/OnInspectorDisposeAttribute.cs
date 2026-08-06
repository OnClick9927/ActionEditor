using System;

namespace ActionAttribute
{
    /// <summary>标记检查器释放或切换目标时调用的无参方法。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorDisposeAttribute : ActionAttributeBase { }
}
