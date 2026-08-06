using System;

namespace ActionAttribute
{
    /// <summary>ShowAssetPreviewAttribute 的别名，显示资源预览缩略图。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PreviewFieldAttribute : ShowAssetPreviewAttribute
    {
        public PreviewFieldAttribute(int width = 96, int height = 96)
            : base(width, height) { }
    }
}
