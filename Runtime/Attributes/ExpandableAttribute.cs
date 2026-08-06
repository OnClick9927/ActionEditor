using System;

namespace ActionAttribute
{
    /// <summary>允许在当前检查器中展开并编辑引用对象。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ExpandableAttribute : ActionAttributeBase { }
}
