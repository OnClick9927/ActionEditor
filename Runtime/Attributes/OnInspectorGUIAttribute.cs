using System;

namespace ActionAttribute
{
    /// <summary>标记每次检查器 GUI 绘制期间调用的无参方法。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorGUIAttribute : ActionAttributeBase { }
}
