using System;

namespace ActionAttribute
{
    /// <summary>在绘制字段期间临时修改 GUI 的颜色和透明度。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GUIColorAttribute : ActionAttributeBase
    {
        public readonly float red;
        public readonly float green;
        public readonly float blue;
        public readonly float alpha;

        public GUIColorAttribute(float red, float green, float blue,
            float alpha = 1)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
        }
    }
}
