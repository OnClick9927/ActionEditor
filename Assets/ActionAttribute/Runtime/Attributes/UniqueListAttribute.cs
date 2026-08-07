using System;

namespace ActionAttribute
{
    /// <summary>校验数组或列表中不存在相等的重复元素。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UniqueListAttribute : ActionAttributeBase
    {
        public readonly string message;

        public UniqueListAttribute(string message = null)
        {
            this.message = message;
        }
    }
}
