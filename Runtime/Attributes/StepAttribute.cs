using System;

namespace ActionAttribute
{
    /// <summary>将整数或浮点字段吸附到相对指定原点的固定步长。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StepAttribute : ActionAttributeBase
    {
        public readonly double step;
        public readonly double origin;

        public StepAttribute(double step, double origin = 0)
        {
            this.step = Math.Abs(step);
            this.origin = origin;
        }
    }
}
