using System;

namespace ActionAttribute
{
    /// <summary>将枚举选项绘制为一组切换按钮。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EnumToggleButtonsAttribute : ActionAttributeBase { }
}
