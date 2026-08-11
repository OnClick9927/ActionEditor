using System;

namespace ActionAttribute
{
    /// <summary>指定条件匹配时对字段执行的显示或编辑行为。</summary>
    public enum ConditionMode
    {
        Show,
        Hide,
        Enable,
        Disable
    }

    /// <summary>根据成员值、多个成员组合或 Inspector 模式控制字段。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ConditionAttribute : ActionAttributeBase
    {
        public readonly ConditionMode mode;
        public readonly string[] conditions;
        public readonly ConditionOperator conditionOperator;
        public readonly object expected;
        public readonly InspectorMode inspectorMode;
        public readonly bool usesInspectorMode;

        public ConditionAttribute(ConditionMode mode, string condition)
            : this(mode, condition, true) { }

        public ConditionAttribute(ConditionMode mode, string condition,
            object expected)
        {
            this.mode = mode;
            conditions = new[] { condition };
            conditionOperator = ConditionOperator.And;
            this.expected = expected;
            inspectorMode = InspectorMode.Always;
        }

        public ConditionAttribute(ConditionMode mode,
            ConditionOperator conditionOperator, params string[] conditions)
        {
            this.mode = mode;
            this.conditions = conditions ?? Array.Empty<string>();
            this.conditionOperator = conditionOperator;
            expected = true;
            inspectorMode = InspectorMode.Always;
        }

        public ConditionAttribute(ConditionMode mode,
            InspectorMode inspectorMode)
        {
            this.mode = mode;
            conditions = Array.Empty<string>();
            conditionOperator = ConditionOperator.And;
            expected = true;
            this.inspectorMode = inspectorMode;
            usesInspectorMode = true;
        }
    }
}
