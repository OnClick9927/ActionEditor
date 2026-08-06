using System;

namespace ActionAttribute
{
    /// <summary>将枚举字段按位标志掩码进行绘制。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EnumFlagsAttribute : ActionAttributeBase
    {
    }
}
