using System;

namespace ActionAttribute
{
    /// <summary>限制 Unity 对象字段只能选择当前场景中的对象。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SceneObjectsOnlyAttribute : ActionAttributeBase { }
}
