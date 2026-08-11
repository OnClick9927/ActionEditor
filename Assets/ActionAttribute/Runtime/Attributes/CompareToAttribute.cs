using System;

namespace ActionAttribute
{
    public enum ValueRelation
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    /// <summary>校验当前值与同一 Owner 上另一个成员的关系。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class CompareToAttribute : ActionValidationAttribute
    {
        public readonly string member;
        public readonly ValueRelation relation;

        public CompareToAttribute(string member, ValueRelation relation,
            string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
            : base(message, type)
        {
            this.member = member ?? string.Empty;
            this.relation = relation;
        }

        public override bool IsValid(ActionAttributeContext context)
        {
            if (!context.TryGetMemberValue(member, out object other))
                return false;
            bool equal = AreEqual(context.Value, other);
            if (relation == ValueRelation.Equal) return equal;
            if (relation == ValueRelation.NotEqual) return !equal;
            if (!TryCompare(context.Value, other, out int comparison))
                return false;
            switch (relation)
            {
                case ValueRelation.LessThan: return comparison < 0;
                case ValueRelation.LessThanOrEqual: return comparison <= 0;
                case ValueRelation.GreaterThan: return comparison > 0;
                case ValueRelation.GreaterThanOrEqual: return comparison >= 0;
                default: return false;
            }
        }

        public override string GetMessage(ActionAttributeContext context)
        {
            return string.IsNullOrEmpty(message)
                ? $"{context.MemberName} 必须满足 {relation} {member}。"
                : message;
        }

        private static bool AreEqual(object left, object right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null) return false;
            if (TryCompareNumbers(left, right, out int comparison))
                return comparison == 0;
            return Equals(left, right);
        }

        private static bool TryCompare(object left, object right,
            out int comparison)
        {
            comparison = 0;
            if (left == null || right == null) return false;
            if (TryCompareNumbers(left, right, out comparison)) return true;
            if (left is string leftText && right is string rightText)
            {
                comparison = string.Compare(leftText, rightText,
                    StringComparison.Ordinal);
                return true;
            }
            if (!(left is IComparable comparable)) return false;
            object converted = right;
            Type leftType = left.GetType();
            if (!leftType.IsInstanceOfType(right))
            {
                try
                {
                    converted = leftType.IsEnum
                        ? right is string text
                            ? Enum.Parse(leftType, text, true)
                            : Enum.ToObject(leftType, right)
                        : Convert.ChangeType(right, leftType);
                }
                catch
                {
                    return false;
                }
            }
            try
            {
                comparison = comparable.CompareTo(converted);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCompareNumbers(object left, object right,
            out int comparison)
        {
            comparison = 0;
            Type leftType = left.GetType();
            Type rightType = right.GetType();
            if (!IsNumber(leftType) || !IsNumber(rightType)) return false;
            try
            {
                if (left is float || left is double ||
                    right is float || right is double)
                {
                    comparison = Convert.ToDouble(left).CompareTo(
                        Convert.ToDouble(right));
                    return true;
                }
                comparison = Convert.ToDecimal(left).CompareTo(
                    Convert.ToDecimal(right));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsNumber(Type type)
        {
            if (type.IsEnum) return false;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }
    }
}
