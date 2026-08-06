using System;

namespace ActionAttribute
{
    /// <summary>仅当指定成员满足条件时允许编辑字段。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class EnableIfAttribute : ActionAttributeBase
    {
        public readonly string[] conditions;
        public readonly ConditionOperator conditionOperator;
        public readonly object expected;

        public EnableIfAttribute(string condition)
            : this(condition, true) { }

        public EnableIfAttribute(string condition, object expected)
        {
            conditions = new[] { condition };
            conditionOperator = ConditionOperator.And;
            this.expected = expected;
        }

        public EnableIfAttribute(ConditionOperator conditionOperator,
            params string[] conditions)
        {
            this.conditions = conditions ?? Array.Empty<string>();
            this.conditionOperator = conditionOperator;
            expected = true;
        }
    }
}
