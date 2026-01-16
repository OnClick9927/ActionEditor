using System;

namespace ActionEditor
{
    /// <summary>
    /// 自定义名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class NameAttribute :
#if UNITY_5_3_OR_NEWER
        UnityEngine.PropertyAttribute
#else
        Attribute
#endif
    {
        public readonly string name;

        public NameAttribute(string name)
        {
            this.name = name;
        }
    }


    /// <summary>
    /// 指定附加类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AttachableAttribute : Attribute
    {
        public readonly Type[] Types;

        public AttachableAttribute(params Type[] types)
        {
            this.Types = types;
        }
    }


    /// <summary>
    /// 指定类型的图标
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IconAttribute : Attribute
    {
        public readonly string iconPath;
        public readonly Type fromType;

        public IconAttribute(string iconPath)
        {
            this.iconPath = iconPath;
        }

        public IconAttribute(Type fromType)
        {
            this.fromType = fromType;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute :
#if UNITY_5_3_OR_NEWER
        UnityEngine.PropertyAttribute
#else
        Attribute
#endif
    {

    }
}