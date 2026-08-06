using System;

namespace ActionAttribute
{
    /// <summary>在 Color 字段下方绘制一组可快速选择的十六进制颜色。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ColorPaletteAttribute : ActionAttributeBase
    {
        public readonly string[] colors;

        public ColorPaletteAttribute(params string[] colors)
        {
            this.colors = colors ?? Array.Empty<string>();
        }
    }
}
