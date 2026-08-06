using System;

namespace ActionAttribute
{
    /// <summary>将成员组织到指定分组下的选项卡页面中。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TabGroupAttribute : GroupAttributeBase
    {
        public readonly string tab;

        public TabGroupAttribute(string group, string tab, int order = 0)
            : base(group, order)
        {
            this.tab = tab ?? string.Empty;
        }
    }
}
