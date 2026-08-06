using System;

namespace ActionAttribute
{
    /// <summary>在非运行状态下禁用字段编辑。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DisableInEditorModeAttribute : ActionAttributeBase { }
}
