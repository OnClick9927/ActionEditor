using System;

namespace ActionAttribute
{
    /// <summary>将字符串字段绘制为可在指定行数范围内自适应高度的文本区。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ResizableTextAreaAttribute : ActionAttributeBase
    {
        public readonly int minLines;
        public readonly int maxLines;

        public ResizableTextAreaAttribute(int minLines = 3, int maxLines = 12)
        {
            this.minLines = Math.Max(1, minLines);
            this.maxLines = Math.Max(this.minLines, maxLines);
        }
    }
}
