using System;
using System.Collections;

namespace ActionAttribute
{
    [Flags]
    public enum CollectionItemRule
    {
        None = 0,
        Unique = 1,
        NotNull = 2,
        NotBlank = 4
    }

    /// <summary>Validates collection length and item constraints together.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CollectionAttribute : ActionValidationAttribute
    {
        private enum ValidationIssue
        {
            None,
            WrongType,
            Count,
            Duplicate,
            NullItem,
            BlankItem
        }

        private const CollectionItemRule AllItemRules =
            CollectionItemRule.Unique | CollectionItemRule.NotNull |
            CollectionItemRule.NotBlank;

        public readonly int min;
        public readonly int max;
        public readonly CollectionItemRule itemRules;

        public CollectionAttribute(int min = 0, int max = int.MaxValue,
            CollectionItemRule itemRules = CollectionItemRule.None,
            string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
            : base(message, type)
        {
            if (min < 0 || max < min)
                throw new ArgumentException("Invalid collection length range.");
            if ((itemRules & ~AllItemRules) != 0)
                throw new ArgumentOutOfRangeException(nameof(itemRules));
            this.min = min;
            this.max = max;
            this.itemRules = itemRules;
        }

        public CollectionAttribute(CollectionItemRule itemRules,
            string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
            : this(0, int.MaxValue, itemRules, message, type) { }

        public override bool IsValid(ActionAttributeContext context)
        {
            return GetIssue(context.Value, out _) == ValidationIssue.None;
        }

        public override string GetMessage(ActionAttributeContext context)
        {
            if (!string.IsNullOrEmpty(message)) return message;
            ValidationIssue issue = GetIssue(context.Value, out int count);
            switch (issue)
            {
                case ValidationIssue.Count:
                    return max == int.MaxValue
                        ? $"{context.MemberName} 至少需要 {min} 个元素，当前为 {count} 个。"
                        : $"{context.MemberName} 的元素数量必须在 {min} 到 {max} 之间，当前为 {count} 个。";
                case ValidationIssue.Duplicate:
                    return $"{context.MemberName} 不能包含重复元素。";
                case ValidationIssue.NullItem:
                    return $"{context.MemberName} 不能包含 null 元素。";
                case ValidationIssue.BlankItem:
                    return $"{context.MemberName} 不能包含空白文本元素。";
                case ValidationIssue.WrongType:
                    return $"{context.MemberName} 必须是数组或列表。";
                default:
                    return null;
            }
        }

        private ValidationIssue GetIssue(object value, out int count)
        {
            count = 0;
            if (value == null) return min == 0
                ? ValidationIssue.None
                : ValidationIssue.Count;
            if (value is string || !(value is IList values))
                return ValidationIssue.WrongType;
            count = values.Count;
            if (count < min || count > max) return ValidationIssue.Count;

            for (int i = 0; i < count; i++)
            {
                object item = values[i];
                if ((itemRules & CollectionItemRule.NotNull) != 0 &&
                    item == null)
                    return ValidationIssue.NullItem;
                if ((itemRules & CollectionItemRule.NotBlank) != 0 &&
                    (!(item is string text) || string.IsNullOrWhiteSpace(text)))
                    return ValidationIssue.BlankItem;
                if ((itemRules & CollectionItemRule.Unique) == 0) continue;
                for (int j = 0; j < i; j++)
                    if (Equals(values[j], item))
                        return ValidationIssue.Duplicate;
            }
            return ValidationIssue.None;
        }
    }
}
