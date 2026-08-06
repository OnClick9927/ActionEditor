using System;

namespace ActionAttribute
{
    /// <summary>将同组成员组织在带标题和可选副标题的区域中。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TitleGroupAttribute : GroupAttributeBase
    {
        public readonly string subtitle;

        public TitleGroupAttribute(string group, string subtitle = null,
            int order = 0) : base(group, order)
        {
            this.subtitle = subtitle;
        }
    }
}
