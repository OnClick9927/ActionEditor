using System;

namespace ActionAttribute
{
    /// <summary>在字段位置绘制一条具有指定高度和边距的分隔线。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HorizontalLineAttribute : ActionAttributeBase
    {
        public readonly float height;
        public readonly float margin;

        public HorizontalLineAttribute(float height = 1, float margin = 6)
        {
            this.height = Math.Max(1, height);
            this.margin = Math.Max(0, margin);
        }
    }
}
