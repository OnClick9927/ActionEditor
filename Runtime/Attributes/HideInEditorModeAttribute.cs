using System;

namespace ActionAttribute
{
    /// <summary>在非运行状态下隐藏字段。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HideInEditorModeAttribute : ActionAttributeBase { }
}
