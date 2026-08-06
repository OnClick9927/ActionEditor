using System;

namespace ActionAttribute
{
    /// <summary>当指定成员满足条件时禁用字段编辑，但仍显示字段。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class DisableIfAttribute : ActionAttributeBase
    {
        public readonly string[] conditions;
        public readonly ConditionOperator conditionOperator;
        public readonly object expected;

        public DisableIfAttribute(string condition)
            : this(condition, true) { }

        public DisableIfAttribute(string condition, object expected)
        {
            conditions = new[] { condition };
            conditionOperator = ConditionOperator.And;
            this.expected = expected;
        }

        public DisableIfAttribute(ConditionOperator conditionOperator,
            params string[] conditions)
        {
            this.conditions = conditions ?? Array.Empty<string>();
            this.conditionOperator = conditionOperator;
            expected = true;
        }
    }
}
