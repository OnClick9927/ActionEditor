using System;

namespace ActionAttribute
{
    /// <summary>将字符串字段绘制为密码输入框，Inspector 中不显示明文。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PasswordFieldAttribute : ActionAttributeBase { }
}
