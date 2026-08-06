using System;

namespace ActionAttribute
{
    /// <summary>EnumSearchAttribute 的别名，为枚举字段提供搜索选择窗口。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SearchableAttribute : EnumSearchAttribute
    {
        public SearchableAttribute(int minimumItemCount = 0)
            : base(minimumItemCount) { }
    }
}
