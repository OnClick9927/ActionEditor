using System;

namespace ActionUnity
{
    public abstract class ActionAttributeBase:
#if UNITY_EDITOR
        UnityEngine.PropertyAttribute
#else
        Attribute
#endif
    {

    }

    /// <summary>
    /// 自定义名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class NameAttribute : ActionAttributeBase
    {
        public readonly string name;
        public readonly string comment;

        public NameAttribute(string name, string comment = null)
        {
            this.name = name;
            this.comment = comment;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : ActionAttributeBase
    {
    }

    /// <summary>
    /// 仅在同级布尔字段等于指定值时绘制字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ShowIfAttribute : ActionAttributeBase
    {
        public readonly string condition;
        public readonly bool expected;

        public ShowIfAttribute(string condition, bool expected = true)
        {
            this.condition = condition;
            this.expected = expected;
        }
    }

    /// <summary>
    /// 仅在同级布尔字段等于指定值时允许编辑字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class EnableIfAttribute : ActionAttributeBase
    {
        public readonly string condition;
        public readonly bool expected;

        public EnableIfAttribute(string condition, bool expected = true)
        {
            this.condition = condition;
            this.expected = expected;
        }
    }

    /// <summary>
    /// 将整数或浮点字段限制在指定范围内。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ClampAttribute : ActionAttributeBase
    {
        public readonly double min;
        public readonly double max;

        public ClampAttribute(double min, double max)
        {
            if (min > max)
                throw new ArgumentException("min cannot be greater than max");
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// 将字符串绘制为指定行数的多行文本框。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MultilineTextAttribute : ActionAttributeBase
    {
        public readonly int lines;

        public MultilineTextAttribute(int lines = 3)
        {
            this.lines = Math.Max(1, lines);
        }
    }

    public enum InspectorMessageType
    {
        None,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 在字段上方绘制说明信息。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class HelpBoxAttribute : ActionAttributeBase
    {
        public readonly string message;
        public readonly InspectorMessageType type;

        public HelpBoxAttribute(string message,
            InspectorMessageType type = InspectorMessageType.Info)
        {
            this.message = message;
            this.type = type;
        }
    }

    /// <summary>
    /// 在对象引用或字符串为空时显示校验错误。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : ActionAttributeBase
    {
        public readonly string message;

        public RequiredAttribute(string message = null)
        {
            this.message = message;
        }
    }

    /// <summary>
    /// 指定类型的图标
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IconAttribute : System.Attribute
    {
        public readonly string iconPath;
        public readonly string base64;
        public readonly Type fromType;

        public IconAttribute(string value, bool isBase64 = false)
        {
            if (isBase64 || IsBase64Value(value))
                base64 = value;
            else
                iconPath = value;
        }

        public IconAttribute(Type fromType)
        {
            this.fromType = fromType;
        }

        private static bool IsBase64Value(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                (value.StartsWith("base64:", StringComparison.OrdinalIgnoreCase) ||
                 value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase));
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
}
