using System;

namespace ActionAttribute
{
    /// <summary>FoldoutGroupAttribute 的简写形式，将同组成员放入可折叠区域。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FoldoutAttribute : FoldoutGroupAttribute
    {
        public FoldoutAttribute(string group, bool expanded = true,
            int order = 0) : base(group, expanded, order) { }
    }
}
