using System;

namespace ActionAttribute
{
    /// <summary>将同组成员横向排列，并可指定成员宽度。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HorizontalGroupAttribute : GroupAttributeBase
    {
        public readonly float width;

        public HorizontalGroupAttribute(string group, float width = 0,
            int order = 0) : base(group, order)
        {
            this.width = Math.Max(0, width);
        }
    }
}
