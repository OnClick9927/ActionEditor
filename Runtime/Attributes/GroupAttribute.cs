using System;

namespace ActionAttribute
{
    /// <summary>VerticalGroupAttribute 的简写形式，按名称纵向组织成员。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GroupAttribute : VerticalGroupAttribute
    {
        public GroupAttribute(string group, int order = 0)
            : base(group, order) { }
    }
}
