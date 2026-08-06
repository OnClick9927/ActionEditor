using System;

namespace ActionAttribute
{
    /// <summary>在自定义检查器中显示指定的非序列化字段。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowNonSerializedFieldAttribute :
        ShowInInspectorAttribute { }
}
