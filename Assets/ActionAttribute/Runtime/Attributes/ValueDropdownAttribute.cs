using System;
using System.Collections.Generic;

namespace ActionAttribute
{
    /// <summary>指定可搜索下拉列表的候选值来源。</summary>
    public enum ValueDropdownSource
    {
        Auto,
        Member,
        Search,
        Tag,
        Layer,
        SortingLayer,
        Scene,
        InputAxis,
        AnimatorParameter
    }

    /// <summary>从成员或 Unity 项目数据读取候选值并绘制可搜索列表。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ValueDropdownAttribute : ActionAttributeBase
    {
        public readonly ValueDropdownSource source;
        public readonly string valuesMember;
        public readonly bool includeDisabled;

        public ValueDropdownAttribute(string valuesMember = null)
        {
            this.valuesMember = valuesMember;
            source = string.IsNullOrEmpty(valuesMember)
                ? ValueDropdownSource.Auto
                : ValueDropdownSource.Member;
        }

        public ValueDropdownAttribute(ValueDropdownSource source,
            string sourceMember = null, bool includeDisabled = false)
        {
            this.source = source;
            valuesMember = sourceMember;
            this.includeDisabled = includeDisabled;
        }
    }

    /// <summary>表示下拉列表中独立的显示文本和值。</summary>
    public readonly struct ValueDropdownItem<T>
    {
        public readonly string text;
        public readonly T value;

        public ValueDropdownItem(string text, T value)
        {
            this.text = text;
            this.value = value;
        }
    }

    /// <summary>提供可通过显示文本和值快速添加选项的下拉数据列表。</summary>
    public sealed class ValueDropdownList<T> :
        List<ValueDropdownItem<T>>
    {
        public void Add(string text, T value) =>
            Add(new ValueDropdownItem<T>(text, value));
    }
}
