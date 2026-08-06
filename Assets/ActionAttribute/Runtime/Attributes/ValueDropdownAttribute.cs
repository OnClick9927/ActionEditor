using System;
using System.Collections.Generic;

namespace ActionAttribute
{
    /// <summary>从目标对象的指定成员读取候选值，并将字段绘制为下拉列表。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ValueDropdownAttribute : ActionAttributeBase
    {
        public readonly string valuesMember;

        public ValueDropdownAttribute(string valuesMember)
        {
            this.valuesMember = valuesMember;
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
