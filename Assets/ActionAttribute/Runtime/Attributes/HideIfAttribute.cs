using System;

namespace ActionAttribute
{
    /// <summary>当指定成员满足条件时隐藏字段。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class HideIfAttribute : ActionAttributeBase
    {
        public readonly string[] conditions;
        public readonly ConditionOperator conditionOperator;
        public readonly object expected;

        public HideIfAttribute(string condition)
            : this(condition, true) { }

        public HideIfAttribute(string condition, object expected)
        {
            conditions = new[] { condition };
            conditionOperator = ConditionOperator.And;
            this.expected = expected;
        }

        public HideIfAttribute(ConditionOperator conditionOperator,
            params string[] conditions)
        {
            this.conditions = conditions ?? Array.Empty<string>();
            this.conditionOperator = conditionOperator;
            expected = true;
        }
    }
}
