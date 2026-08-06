using System;

namespace ActionAttribute
{
    /// <summary>将数组或列表绘制为可配置拖拽、添加和删除的列表。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReorderableListAttribute : ActionAttributeBase
    {
        public readonly bool draggable;
        public readonly bool add;
        public readonly bool remove;

        public ReorderableListAttribute(bool draggable = true, bool add = true,
            bool remove = true)
        {
            this.draggable = draggable;
            this.add = add;
            this.remove = remove;
        }
    }
}
