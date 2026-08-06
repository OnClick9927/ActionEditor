using System;

namespace ActionAttribute
{
    /// <summary>以只读状态显示字段，防止在检查器中修改。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : ActionAttributeBase
    {
    }
}
