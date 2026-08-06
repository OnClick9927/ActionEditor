using System;

namespace ActionAttribute
{
    /// <summary>ButtonGroupAttribute 的响应式版本，使同组按钮按可用宽度自动布局。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ResponsiveButtonGroupAttribute : ButtonGroupAttribute
    {
        public ResponsiveButtonGroupAttribute(string group) : base(group) { }
    }
}
