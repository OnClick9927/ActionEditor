using System;

namespace ActionAttribute
{
    /// <summary>指定 Inspector 中一组字段的布局方式。</summary>
    public enum GroupType
    {
        Vertical,
        Horizontal,
        Box,
        Foldout,
        Tab,
        Title,
        Toggle,
        Button,
        FoldoutBox,
        Grid,
        Indent,
        Scroll
    }

    /// <summary>
    /// 将同名字段组织成指定布局；条件显示可与 Condition 组合使用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class GroupAttribute : ActionAttributeBase
    {
        public readonly string group;
        public readonly GroupType type;

        public float Width { get; set; }
        public float Height { get; set; } = 180;
        public int Columns { get; set; } = 2;
        public bool Expanded { get; set; } = true;
        public bool ShowLabel { get; set; } = true;
        public string Tab { get; set; }
        public string Subtitle { get; set; }
        public string ToggleMember { get; set; }

        public GroupAttribute(string group, GroupType type = GroupType.Vertical)
        {
            this.group = group ?? string.Empty;
            this.type = type;
        }
    }
}
