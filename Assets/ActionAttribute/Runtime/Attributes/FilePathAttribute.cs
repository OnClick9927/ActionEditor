using System;

namespace ActionAttribute
{
    /// <summary>为字符串字段提供文件选择器，并可限制扩展名和路径格式。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FilePathAttribute : ActionAttributeBase
    {
        public readonly string extension;
        public readonly bool absolutePath;

        public FilePathAttribute(string extension = null, bool absolutePath = false)
        {
            this.extension = extension;
            this.absolutePath = absolutePath;
        }
    }
}
