using System;

namespace ActionAttribute
{
    /// <summary>为字符串字段提供文件夹选择器，并可返回绝对路径。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FolderPathAttribute : ActionAttributeBase
    {
        public readonly bool absolutePath;

        public FolderPathAttribute(bool absolutePath = false)
        {
            this.absolutePath = absolutePath;
        }
    }
}
