using System;

namespace ActionAttribute
{
    /// <summary>隐藏 MonoBehaviour 或 ScriptableObject 检查器中的脚本引用行。</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class HideMonoScriptAttribute : ActionAttributeBase { }
}
