using System;

namespace ActionAttribute
{
    /// <summary>限制 Unity 对象字段只能选择项目中的持久化资源。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AssetsOnlyAttribute : ActionAttributeBase { }
}
