using System;

namespace ActionAttribute
{
    /// <summary>当指定成员满足条件时隐藏整个成员分组。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HideIfGroupAttribute : GroupAttributeBase
    {
        public readonly string condition;

        public HideIfGroupAttribute(string group, string condition, int order = 0)
            : base(group, order)
        {
            this.condition = condition;
        }
    }
}
