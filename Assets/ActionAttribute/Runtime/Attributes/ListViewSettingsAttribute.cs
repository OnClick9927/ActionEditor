using System;

namespace ActionAttribute
{
    /// <summary>ReorderableListAttribute 的别名，配置列表拖拽、添加和删除能力。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ListViewSettingsAttribute : ReorderableListAttribute
    {
        public ListViewSettingsAttribute(bool draggable = true, bool add = true,
            bool remove = true) : base(draggable, add, remove) { }
    }
}
