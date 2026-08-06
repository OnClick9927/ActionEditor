using System;

namespace ActionAttribute
{
    /// <summary>将同组成员放入可配置默认展开状态的折叠区域。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FoldoutGroupAttribute : GroupAttributeBase
    {
        public readonly bool expanded;

        public FoldoutGroupAttribute(string group, bool expanded = true,
            int order = 0) : base(group, order)
        {
            this.expanded = expanded;
        }
    }
}
