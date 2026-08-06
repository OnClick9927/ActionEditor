using System;

namespace ActionAttribute
{
    /// <summary>隐藏字段左侧的标签，仅绘制字段内容。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HideLabelAttribute : ActionAttributeBase
    {
    }
}
