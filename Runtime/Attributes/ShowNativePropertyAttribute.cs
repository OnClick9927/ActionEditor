using System;

namespace ActionAttribute
{
    /// <summary>在自定义检查器中显示指定的原生属性。</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ShowNativePropertyAttribute :
        ShowInInspectorAttribute { }
}
