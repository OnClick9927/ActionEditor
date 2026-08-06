using System;

namespace ActionAttribute
{
    /// <summary>HelpBoxAttribute 的别名，在字段附近显示信息提示框。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class InfoBoxAttribute : HelpBoxAttribute
    {
        public InfoBoxAttribute(string message,
            InspectorMessageType type = InspectorMessageType.Info)
            : base(message, type) { }
    }
}
