using System;

namespace ActionAttribute
{
    /// <summary>ExpandableAttribute 的别名，在当前检查器中内联绘制引用对象。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InlineEditorAttribute : ExpandableAttribute { }
}
