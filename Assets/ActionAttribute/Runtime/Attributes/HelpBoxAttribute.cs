using System;

namespace ActionAttribute
{
    /// <summary>指定检查器提示框不显示图标，或使用信息、警告、错误级别。</summary>
    public enum InspectorMessageType
    {
        None,
        Info,
        Warning,
        Error
    }

    /// <summary>在字段附近显示指定级别的帮助提示框。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class HelpBoxAttribute : ActionAttributeBase
    {
        public readonly string message;
        public readonly InspectorMessageType type;

        public HelpBoxAttribute(string message,
            InspectorMessageType type = InspectorMessageType.Info)
        {
            this.message = message;
            this.type = type;
        }
    }
}
