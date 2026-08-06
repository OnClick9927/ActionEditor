using System;

namespace ActionAttribute
{
    /// <summary>为枚举字段提供可搜索的候选项窗口。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumSearchAttribute : ActionAttributeBase
    {
        public readonly int minimumItemCount;

        public EnumSearchAttribute(int minimumItemCount = 0)
        {
            this.minimumItemCount = Math.Max(0, minimumItemCount);
        }
    }
}
