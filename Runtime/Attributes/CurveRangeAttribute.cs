using System;

namespace ActionAttribute
{
    /// <summary>限制 AnimationCurve 的可视范围并指定曲线显示颜色。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CurveRangeAttribute : ActionAttributeBase
    {
        public readonly float minX;
        public readonly float minY;
        public readonly float maxX;
        public readonly float maxY;
        public readonly float red;
        public readonly float green;
        public readonly float blue;

        public CurveRangeAttribute(float minX = 0, float minY = 0,
            float maxX = 1, float maxY = 1, float red = 0.3f,
            float green = 0.7f, float blue = 1)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            this.red = red;
            this.green = green;
            this.blue = blue;
        }
    }
}
