using System;

namespace ActionAttribute
{
    /// <summary>在字段输入控件右侧绘制单位或补充说明文本。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SuffixLabelAttribute : ActionAttributeBase
    {
        public readonly string label;

        public SuffixLabelAttribute(string label)
        {
            this.label = label;
        }
    }
}
