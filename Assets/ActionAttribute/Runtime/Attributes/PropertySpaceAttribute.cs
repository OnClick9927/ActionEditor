using System;

namespace ActionAttribute
{
    /// <summary>在字段前后增加指定高度的垂直间距。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PropertySpaceAttribute : ActionAttributeBase
    {
        public readonly float before;
        public readonly float after;

        public PropertySpaceAttribute(float before = 8, float after = 0)
        {
            this.before = Math.Max(0, before);
            this.after = Math.Max(0, after);
        }
    }
}
