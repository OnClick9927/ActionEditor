using System;

namespace ActionAttribute
{
    /// <summary>仅当指定成员满足条件时显示整个成员分组。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowIfGroupAttribute : GroupAttributeBase
    {
        public readonly string condition;

        public ShowIfGroupAttribute(string group, string condition, int order = 0)
            : base(group, order)
        {
            this.condition = condition;
        }
    }
}
