using System;

namespace ActionAttribute
{
    /// <summary>在被标记类型的检查器顶部显示指定级别的说明框。</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true,
        Inherited = true)]
    public sealed class TypeInfoBoxAttribute : ActionAttributeBase
    {
        public readonly string message;
        public readonly InspectorMessageType type;

        public TypeInfoBoxAttribute(string message,
            InspectorMessageType type = InspectorMessageType.Info)
        {
            this.message = message;
            this.type = type;
        }
    }
}
