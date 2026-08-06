using System;

namespace ActionAttribute
{
    /// <summary>替换成员或类型的显示名称，并将注释作为 GUIContent 提示文本。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field |
        AttributeTargets.Property)]
    public class NameAttribute : ActionAttributeBase
    {
        public readonly string name;
        public readonly string comment;

        public NameAttribute(string name, string comment = null)
        {
            this.name = name;
            this.comment = comment;
        }
    }
}
