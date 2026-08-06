using System;

namespace ActionAttribute
{
    /// <summary>所有 ActionAttribute 检查器绘制特性的公共基类；在 Unity 编辑器中同时作为 PropertyAttribute 使用。</summary>
    public abstract class ActionAttributeBase :
#if UNITY_EDITOR
        UnityEngine.PropertyAttribute
#else
        Attribute
#endif
    {
    }
}
