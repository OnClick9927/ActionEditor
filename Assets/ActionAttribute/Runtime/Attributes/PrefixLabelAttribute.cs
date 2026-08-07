using System;

namespace ActionAttribute
{
    /// <summary>在标准字段标签与输入控件之间显示简短的单位或说明文本。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PrefixLabelAttribute : ActionAttributeBase
    {
        public readonly string label;

        public PrefixLabelAttribute(string label)
        {
            this.label = label ?? string.Empty;
        }
    }
}
