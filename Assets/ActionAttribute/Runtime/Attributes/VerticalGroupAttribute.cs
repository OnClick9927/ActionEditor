using System;

namespace ActionAttribute
{
    /// <summary>将同组成员按指定顺序纵向排列。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class VerticalGroupAttribute : GroupAttributeBase
    {
        public VerticalGroupAttribute(string group, int order = 0)
            : base(group, order) { }
    }
}
