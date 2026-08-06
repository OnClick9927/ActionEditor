using System;

namespace ActionAttribute
{
    /// <summary>使用项目资源选择器编辑字符串，并保存资源的稳定 GUID。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AssetGuidAttribute : ActionAttributeBase
    {
        public readonly Type assetType;

        public AssetGuidAttribute(Type assetType = null)
        {
            this.assetType = assetType;
        }
    }
}
