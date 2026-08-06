using System;

namespace ActionAttribute
{
    /// <summary>在字段上方绘制标题和可选副标题。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TitleAttribute : ActionAttributeBase
    {
        public readonly string title;
        public readonly string subtitle;

        public TitleAttribute(string title, string subtitle = null)
        {
            this.title = title;
            this.subtitle = subtitle;
        }
    }
}
