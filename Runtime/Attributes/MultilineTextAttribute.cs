using System;

namespace ActionAttribute
{
    /// <summary>将字符串字段绘制为固定行数的多行文本框。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MultilineTextAttribute : ActionAttributeBase
    {
        public readonly int lines;

        public MultilineTextAttribute(int lines = 3)
        {
            this.lines = Math.Max(1, lines);
        }
    }
}
