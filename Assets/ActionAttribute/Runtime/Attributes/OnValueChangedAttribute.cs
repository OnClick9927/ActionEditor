using System;

namespace ActionAttribute
{
    /// <summary>字段值提交后调用目标对象上的指定回调方法。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class OnValueChangedAttribute : ActionAttributeBase
    {
        public readonly string callback;

        public OnValueChangedAttribute(string callback)
        {
            this.callback = callback;
        }
    }
}
