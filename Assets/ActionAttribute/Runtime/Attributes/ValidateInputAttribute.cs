using System;

namespace ActionAttribute
{
    /// <summary>调用指定校验方法检查字段值，并在失败时显示提示。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ValidateInputAttribute : ActionAttributeBase
    {
        public readonly string callback;
        public readonly string message;
        public readonly InspectorMessageType type;

        public ValidateInputAttribute(string callback, string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
        {
            this.callback = callback;
            this.message = message;
            this.type = type;
        }
    }
}
