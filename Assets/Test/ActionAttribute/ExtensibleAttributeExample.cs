using System;
using UnityEngine;

namespace ActionAttribute.Examples
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class PositiveValueConditionAttribute :
        ActionConditionAttribute
    {
        public PositiveValueConditionAttribute(ConditionMode mode)
            : base(mode) { }

        public override bool Evaluate(ActionAttributeContext context)
        {
            return context.TryGetValue<double>(out double value) && value > 0;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class EvenValueAttribute : ActionValidationAttribute
    {
        public EvenValueAttribute(string message = "数值必须是偶数。")
            : base(message) { }

        public override bool IsValid(ActionAttributeContext context)
        {
            return context.TryGetValue<long>(out long value) && value % 2 == 0;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ScaleValueAttribute : ActionValueModifierAttribute
    {
        private readonly double factor;

        public ScaleValueAttribute(double factor)
        {
            this.factor = factor;
        }

        public override object Modify(ActionAttributeContext context)
        {
            return context.TryGetValue<double>(out double value)
                ? Convert.ChangeType(value * factor, context.ValueType)
                : context.Value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class OffsetValueAttribute : ActionValueModifierAttribute
    {
        private readonly double offset;

        public OffsetValueAttribute(double offset)
        {
            this.offset = offset;
        }

        public override object Modify(ActionAttributeContext context)
        {
            return context.TryGetValue<double>(out double value)
                ? Convert.ChangeType(value + offset, context.ValueType)
                : context.Value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class LimitMessageAttribute : ActionMessageAttribute
    {
        private readonly double maximum;

        public LimitMessageAttribute(double maximum)
        {
            this.maximum = maximum;
        }

        public override ActionMessage GetMessage(ActionAttributeContext context)
        {
            return context.TryGetValue<double>(out double value) &&
                value > maximum
                    ? new ActionMessage($"当前值超过建议上限 {maximum}。",
                        InspectorMessageType.Warning)
                    : default;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CurrentValueLabelAttribute : ActionLabelAttribute
    {
        private readonly string prefix;

        public CurrentValueLabelAttribute(string prefix)
        {
            this.prefix = prefix ?? string.Empty;
        }

        public override string GetLabel(ActionAttributeContext context)
        {
            return $"{prefix} ({context.Value})";
        }

        public override string GetTooltip(ActionAttributeContext context)
        {
            return context.PropertyPath;
        }
    }

    public sealed class ExtensibleAttributeExample : MonoBehaviour
    {
        [Slider(0, 100)] public int percent = 150;
        [PositiveValueCondition(ConditionMode.Show)]
        public int positiveOnly = -1;
        [EvenValue] public int evenValue = 3;
        [ScaleValue(2, Priority = -10), OffsetValue(3, Priority = 10)]
        public int orderedValue = 4;
        [LimitMessage(10)] public int recommendedValue = 12;
        [CurrentValueLabel("实时数值")] public int labeledValue = 6;
    }
}
