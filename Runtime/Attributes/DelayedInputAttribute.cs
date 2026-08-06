using System;

namespace ActionAttribute
{
    /// <summary>使用延迟输入框，仅在确认输入或失去焦点后提交字段值。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DelayedInputAttribute : ActionAttributeBase
    {
    }
}
