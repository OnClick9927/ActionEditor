using System;

namespace ActionAttribute
{
    /// <summary>当对象引用或字符串为空时在检查器中显示校验错误。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : ActionAttributeBase
    {
        public readonly string message;

        public RequiredAttribute(string message = null)
        {
            this.message = message;
        }
    }
}
