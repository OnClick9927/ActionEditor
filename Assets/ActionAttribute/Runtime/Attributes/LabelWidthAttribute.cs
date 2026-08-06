using System;

namespace ActionAttribute
{
    /// <summary>为字段标签设置临时的固定宽度。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LabelWidthAttribute : ActionAttributeBase
    {
        public readonly float width;

        public LabelWidthAttribute(float width)
        {
            this.width = Math.Max(0, width);
        }
    }
}
