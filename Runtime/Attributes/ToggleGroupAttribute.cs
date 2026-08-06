using System;

namespace ActionAttribute
{
    /// <summary>使用指定布尔成员控制整个分组的启用状态。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ToggleGroupAttribute : GroupAttributeBase
    {
        public readonly string toggleMember;

        public ToggleGroupAttribute(string toggleMember, string group = null,
            int order = 0) : base(group ?? toggleMember, order)
        {
            this.toggleMember = toggleMember;
        }
    }
}
