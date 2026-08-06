using System;

namespace ActionAttribute
{
    /// <summary>在对象引用字段下方显示指定尺寸的资源预览。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowAssetPreviewAttribute : ActionAttributeBase
    {
        public readonly int width;
        public readonly int height;

        public ShowAssetPreviewAttribute(int width = 96, int height = 96)
        {
            this.width = Math.Max(16, width);
            this.height = Math.Max(16, height);
        }
    }
}
