using System;

namespace ActionAttribute
{
    /// <summary>定义数值字段的绘制方式或越界处理方式。</summary>
    public enum SliderMode
    {
        Slider,
        Clamp,
        Wrap,
        Minimum,
        Maximum,
        NonNegative,
        Positive,
        Step
    }

    /// <summary>
    /// 将整数或浮点数字段绘制为滑杆，或对其应用范围、环绕及单边限制。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SliderAttribute : ActionAttributeBase
    {
        public readonly double min;
        public readonly double max;
        public readonly SliderMode mode;
        public double Step { get; set; }
        public double Origin { get; set; }

        public SliderAttribute(double min, double max,
            SliderMode mode = SliderMode.Slider)
        {
            if (mode != SliderMode.Slider && mode != SliderMode.Clamp &&
                mode != SliderMode.Wrap)
                mode = SliderMode.Slider;
            if (double.IsNaN(min)) min = 0;
            if (double.IsNaN(max)) max = min;
            this.min = Math.Min(min, max);
            this.max = Math.Max(min, max);
            this.mode = mode;
        }

        public SliderAttribute(SliderMode mode)
            : this(0, mode) { }

        public SliderAttribute(double value, SliderMode mode)
        {
            if (mode == SliderMode.Step)
            {
                this.mode = mode;
                Step = Math.Abs(value);
                min = double.MinValue;
                max = double.MaxValue;
                return;
            }
            if (mode != SliderMode.Minimum && mode != SliderMode.Maximum &&
                mode != SliderMode.NonNegative && mode != SliderMode.Positive)
            {
                this.mode = SliderMode.Step;
                min = double.MinValue;
                max = double.MaxValue;
                return;
            }
            this.mode = mode;
            min = mode == SliderMode.Minimum ? value : double.MinValue;
            max = mode == SliderMode.Maximum ? value : double.MaxValue;
        }
    }
}
