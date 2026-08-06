using System;

namespace ActionAttribute
{
    /// <summary>使用项目资源选择器编辑字符串，并保存 Assets 开头的资源路径。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AssetPathAttribute : ActionAttributeBase
    {
        public readonly Type assetType;

        public AssetPathAttribute(Type assetType = null)
        {
            this.assetType = assetType;
        }
    }
}
