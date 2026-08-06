using System;

namespace ActionAttribute
{
    /// <summary>所有检查器分组特性的公共基类，保存组名和绘制顺序。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class GroupAttributeBase : ActionAttributeBase
    {
        public readonly string group;
        public readonly int drawOrder;

        protected GroupAttributeBase(string group, int order = 0)
        {
            this.group = group ?? string.Empty;
            drawOrder = order;
        }
    }
}
