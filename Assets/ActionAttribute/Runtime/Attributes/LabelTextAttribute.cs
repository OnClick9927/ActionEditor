using System;

namespace ActionAttribute
{
    /// <summary>NameAttribute 的别名，用自定义文本和提示替换字段标签。</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class LabelTextAttribute : NameAttribute
    {
        public LabelTextAttribute(string text, string tooltip = null)
            : base(text, tooltip) { }
    }
}
