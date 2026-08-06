using System;

namespace ActionAttribute
{
    /// <summary>为字段增加指定级数的检查器缩进。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class IndentAttribute : ActionAttributeBase
    {
        public readonly int level;

        public IndentAttribute(int level = 1)
        {
            this.level = Math.Max(0, level);
        }
    }
}
