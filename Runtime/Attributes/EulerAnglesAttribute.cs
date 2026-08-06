using System;

namespace ActionAttribute
{
    /// <summary>将 Quaternion 字段以便于编辑的欧拉角形式显示。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EulerAnglesAttribute : ActionAttributeBase
    {
    }
}
