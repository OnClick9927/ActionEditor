using System;

namespace ActionAttribute
{
    /// <summary>指定检查器行为应用于编辑模式、运行模式或全部模式。</summary>
    public enum InspectorMode
    {
        Always,
        EditMode,
        PlayMode
    }

    /// <summary>将无参方法绘制为检查器按钮，并可限制按钮的可用模式。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : ActionAttributeBase
    {
        public readonly string text;
        public readonly InspectorMode mode;

        public ButtonAttribute(string text = null,
            InspectorMode mode = InspectorMode.Always)
        {
            this.text = text;
            this.mode = mode;
        }
    }
}
