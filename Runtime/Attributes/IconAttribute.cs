using System;

namespace ActionAttribute
{
    /// <summary>为类型或成员指定图标，支持资源路径、Base64 数据或从其他类型继承图标。</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IconAttribute : Attribute
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
}
