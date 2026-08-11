using System;

namespace ActionAttribute
{
    /// <summary>指定 Unity 对象引用允许的来源或层级范围。</summary>
    public enum ObjectSource
    {
        Assets,
        Scene,
        Children,
        Parents
    }

    /// <summary>限制 Unity 对象字段只能选择指定来源或层级范围的对象。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ObjectsOnlyAttribute : ActionAttributeBase
    {
        public readonly ObjectSource source;
        public readonly bool includeSelf;

        public ObjectsOnlyAttribute(ObjectSource source,
            bool includeSelf = false)
        {
            this.source = source;
            this.includeSelf = includeSelf;
        }
    }
}
