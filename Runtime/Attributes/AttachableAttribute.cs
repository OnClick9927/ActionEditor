using System;

namespace ActionAttribute
{
    /// <summary>声明被标记类型允许附加到的宿主类型集合。</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AttachableAttribute : Attribute
    {
        public readonly Type[] Types;

        public AttachableAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}
