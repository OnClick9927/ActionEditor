using System;

namespace ActionAttribute
{
    /// <summary>在运行状态下禁用字段编辑。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DisableInPlayModeAttribute : ActionAttributeBase { }
}
