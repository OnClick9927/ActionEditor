using System;

namespace ActionAttribute
{
    /// <summary>为字段标签设置检查器悬停提示文本。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PropertyTooltipAttribute : ActionAttributeBase
    {
        public readonly string tooltip;

        public PropertyTooltipAttribute(string tooltip)
        {
            this.tooltip = tooltip;
        }
    }
}
