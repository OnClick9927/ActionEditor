using System;

namespace ActionAttribute
{
    /// <summary>
    /// 要求引用和字符串包含有效值，并要求值类型不等于其默认值。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : ActionValidationAttribute
    {
        [NonSerialized] private Type cachedType;
        [NonSerialized] private object cachedDefault;

        public RequiredAttribute(string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
            : base(message, type) { }

        public override bool IsValid(ActionAttributeContext context)
        {
            object value = context.Value;
            if (IsNull(value)) return false;
            if (value is string text) return !string.IsNullOrWhiteSpace(text);

            Type valueType = context.ValueType ?? value.GetType();
            if (!valueType.IsValueType) return true;
            if (cachedType != valueType)
            {
                cachedType = valueType;
                cachedDefault = Activator.CreateInstance(valueType);
            }
            return !Equals(value, cachedDefault);
        }

        public override string GetMessage(ActionAttributeContext context)
        {
            return string.IsNullOrEmpty(message)
                ? $"{context.MemberName} 不能为空或使用默认值。"
                : message;
        }

        private static bool IsNull(object value)
        {
            if (value == null) return true;
            try
            {
                return value.Equals(null);
            }
            catch
            {
                return false;
            }
        }
    }
}
