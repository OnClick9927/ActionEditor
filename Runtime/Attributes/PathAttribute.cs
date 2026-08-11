using System;

namespace ActionAttribute
{
    /// <summary>指定路径选择器选择文件、文件夹或项目资源。</summary>
    public enum PathType
    {
        File,
        Folder,
        Asset
    }

    /// <summary>为字符串字段提供文件、文件夹或项目资源选择器。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PathAttribute : ActionAttributeBase
    {
        public readonly PathType type;
        public readonly string extension;
        public readonly bool absolutePath;
        public readonly Type assetType;

        public PathAttribute(PathType type = PathType.File,
            string extension = null, bool absolutePath = false,
            Type assetType = null)
        {
            this.type = type;
            this.extension = extension;
            this.absolutePath = absolutePath;
            this.assetType = assetType;
        }

        public PathAttribute(Type assetType)
        {
            type = PathType.Asset;
            this.assetType = assetType;
        }
    }
}
