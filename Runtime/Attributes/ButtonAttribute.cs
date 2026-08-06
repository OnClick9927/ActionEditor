using System;

namespace ActionAttribute
{
    /// <summary>指定检查器按钮在编辑模式、运行模式或任何状态下是否可用。</summary>
    public enum ButtonEnableMode
    {
        Always,
        Editor,
        PlayMode
    }

    /// <summary>将无参方法绘制为检查器按钮，并可限制按钮的可用模式。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : ActionAttributeBase
    {
        public readonly string text;
        public readonly ButtonEnableMode enableMode;

        public ButtonAttribute(string text = null,
            ButtonEnableMode enableMode = ButtonEnableMode.Always)
        {
            this.text = text;
            this.enableMode = enableMode;
        }
    }
}
