using System;

namespace ActionAttribute
{
    /// <summary>将同组成员绘制在带边框的纵向分组中。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class BoxGroupAttribute : GroupAttributeBase
    {
        public readonly bool showLabel;

        public BoxGroupAttribute(string group, bool showLabel = true,
            int order = 0) : base(group, order)
        {
            this.showLabel = showLabel;
        }
    }
}
