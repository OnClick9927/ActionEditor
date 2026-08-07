using System;

namespace ActionAttribute
{
    /// <summary>限制对象引用只能指向当前组件所在对象的子层级。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ChildGameObjectsOnlyAttribute : ActionAttributeBase
    {
        public readonly bool includeSelf;

        public ChildGameObjectsOnlyAttribute(bool includeSelf = false)
        {
            this.includeSelf = includeSelf;
        }
    }
}
