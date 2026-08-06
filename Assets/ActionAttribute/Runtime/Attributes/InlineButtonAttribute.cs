using System;

namespace ActionAttribute
{
    /// <summary>在字段右侧绘制按钮，并在点击时调用指定方法。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class InlineButtonAttribute : ActionAttributeBase
    {
        public readonly string method;
        public readonly string text;

        public InlineButtonAttribute(string method, string text = null)
        {
            this.method = method;
            this.text = text;
        }
    }
}
