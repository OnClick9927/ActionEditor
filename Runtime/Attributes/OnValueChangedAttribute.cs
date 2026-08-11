using System;

namespace ActionAttribute
{
    /// <summary>指定字段值关联方法的执行方式。</summary>
    public enum ValueChangedMode
    {
        Callback,
        Validate
    }

    /// <summary>
    /// 字段值提交后调用回调，或调用布尔方法校验当前字段值。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class OnValueChangedAttribute : ActionAttributeBase
    {
        public readonly string callback;
        public readonly ValueChangedMode mode;
        public readonly string message;
        public readonly InspectorMessageType type;

        public OnValueChangedAttribute(string callback,
            ValueChangedMode mode = ValueChangedMode.Callback,
            string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
        {
            this.callback = callback;
            this.mode = mode;
            this.message = message;
            this.type = type;
        }
    }
}
