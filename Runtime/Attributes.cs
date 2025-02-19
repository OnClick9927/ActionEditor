using System;
using System.Reflection;
using UnityEngine;

namespace ActionEditor
{

    [AttributeUsage(AttributeTargets.Field)]

    public abstract class ValidCheckAttribute : Attribute
    {
        public abstract bool IsValid(FieldInfo field, object obj, out string err);
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class NotNullAttribute : ValidCheckAttribute
    {
        public override bool IsValid(FieldInfo field, object obj, out string err)
        {

            bool error = false;
            if (field.FieldType == typeof(string))
            {
                if (string.IsNullOrEmpty((string)obj))
                {
                    error = true;
                }
            }
            else if (obj == null)
            {
                error = true;
            }
            err = error ? "Can not be NULL" : string.Empty;
            return !error;
        }
    }

    /// <summary>
    /// 选择对象路径
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ObjectPathAttribute : Attribute
    {
        public Type type;

        public ObjectPathAttribute(Type type)
        {
            this.type = type;
        }
    }


    /// <summary>
    /// 自定义名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class NameAttribute : Attribute
    {
        public readonly string name;

        public NameAttribute(string name)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// 指定显示的颜色
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class ColorAttribute : Attribute
    {
        public readonly Color Color;
        public ColorAttribute(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color);
        }
        public ColorAttribute(float r, float g, float b, float a = 1)
        {
            this.Color = new Color(r, g, b, a);
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
    /// 组内唯一性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class UniqueTrackAttribute : Attribute
    {
    }
    /// <summary>
    /// 指定类型的图标
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TrackIconAttribute : Attribute
    {
        public readonly string iconPath;
        public readonly Type fromType;

        public TrackIconAttribute(string iconPath)
        {
            this.iconPath = iconPath;
        }

        public TrackIconAttribute(Type fromType)
        {
            this.fromType = fromType;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : Attribute
    {

    }
}