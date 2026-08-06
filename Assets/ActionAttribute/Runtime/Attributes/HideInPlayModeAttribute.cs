using System;

namespace ActionAttribute
{
    /// <summary>在运行状态下隐藏字段。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HideInPlayModeAttribute : ActionAttributeBase { }
}
