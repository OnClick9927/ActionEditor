using System;

namespace ActionAttribute
{
    /// <summary>将字符串字段绘制为项目 Input Manager 轴名称选择器。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InputAxisAttribute : ActionAttributeBase { }
}
