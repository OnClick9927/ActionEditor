using System;
using UnityEngine;

namespace ActionEditor
{


    /// <summary>
    /// 选择对象路径
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SelectObjectPathAttribute : Attribute
    {
        public Type type;

        public SelectObjectPathAttribute(Type type)
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
    [AttributeUsage(AttributeTargets.Class)]
    public class ColorAttribute : Attribute
    {
        public readonly Color Color;

        public ColorAttribute(float r, float g, float b, float a = 1)
        {
            this.Color = new Color(r, g, b, a);
        }

        public ColorAttribute(Color color)
        {
            this.Color = color;
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
        public readonly Texture2D texture;

        public TrackIconAttribute(Texture2D texture)
        {
            this.texture = texture;
        }

        public TrackIconAttribute(string iconPath)
        {
            this.iconPath = iconPath;
        }

        public TrackIconAttribute(Type fromType)
        {
            this.fromType = fromType;
        }
    }

}