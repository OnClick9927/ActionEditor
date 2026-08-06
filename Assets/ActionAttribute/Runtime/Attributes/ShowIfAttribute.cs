using System;

namespace ActionAttribute
{
    /// <summary>仅当指定成员满足条件时显示字段。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ShowIfAttribute : ActionAttributeBase
    {
        public readonly string[] conditions;
        public readonly ConditionOperator conditionOperator;
        public readonly object expected;

        public ShowIfAttribute(string condition)
            : this(condition, true) { }

        public ShowIfAttribute(string condition, object expected)
        {
            conditions = new[] { condition };
            conditionOperator = ConditionOperator.And;
            this.expected = expected;
        }

        public ShowIfAttribute(ConditionOperator conditionOperator,
            params string[] conditions)
        {
            this.conditions = conditions ?? Array.Empty<string>();
            this.conditionOperator = conditionOperator;
            expected = true;
        }
    }
}
