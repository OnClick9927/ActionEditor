using System;

namespace ActionAttribute
{
    /// <summary>校验数值不是 NaN 或正负无穷。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FiniteAttribute : ActionValidationAttribute
    {
        public FiniteAttribute(string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
            : base(message, type) { }

        public override bool IsValid(ActionAttributeContext context)
        {
            object value = context.Value;
            if (value == null || value.GetType().IsEnum) return false;
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Single:
                    float single = (float)value;
                    return !float.IsNaN(single) && !float.IsInfinity(single);
                case TypeCode.Double:
                    double number = (double)value;
                    return !double.IsNaN(number) && !double.IsInfinity(number);
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        public override string GetMessage(ActionAttributeContext context)
        {
            return string.IsNullOrEmpty(message)
                ? $"{context.MemberName} 必须是有限数值。"
                : message;
        }
    }
}
