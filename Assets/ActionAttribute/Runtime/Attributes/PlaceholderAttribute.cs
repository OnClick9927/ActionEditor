using System;

namespace ActionAttribute
{
    /// <summary>在空字符串输入框中显示不会写入字段的占位提示。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PlaceholderAttribute : ActionAttributeBase
    {
        public readonly string text;

        public PlaceholderAttribute(string text)
        {
            this.text = text;
        }
    }
}
