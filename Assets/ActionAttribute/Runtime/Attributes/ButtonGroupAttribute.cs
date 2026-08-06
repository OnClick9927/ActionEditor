using System;

namespace ActionAttribute
{
    /// <summary>将同组方法按钮排列在同一按钮组中。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonGroupAttribute : ActionAttributeBase
    {
        public readonly string group;

        public ButtonGroupAttribute(string group)
        {
            this.group = group ?? string.Empty;
        }
    }
}
