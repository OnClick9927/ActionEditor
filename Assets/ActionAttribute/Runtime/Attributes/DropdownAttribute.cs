using System;

namespace ActionAttribute
{
    /// <summary>ValueDropdownAttribute 的简写形式，从指定成员读取候选值。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DropdownAttribute : ValueDropdownAttribute
    {
        public DropdownAttribute(string valuesMember) : base(valuesMember) { }
    }
}
