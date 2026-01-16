using System;
using System.Linq;

namespace ActionEditor
{
    public static class SegmentExtensions
    {

  

        public static float Min(float a, float b) => a < b ? a : b;

        public static float Max(float a, float b) => a > b ? a : b;
        public static float Max(float a, float b, float c) => Max(a, Max(b, c));

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }
        public static float ToLocalTime(this ISegment directable, float time) => Clamp(time - directable.StartTime, 0, directable.Length);


        public static float ToLocalTimeUnclamped(this ISegment directable, float time)
        {
            return time - directable.StartTime;
        }

        /// <summary>
        /// 切片能否混合
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static bool CanCrossBlend(this IClip clip, IClip next)
        {
            if (clip == null || next == null) return false;
            if ((clip.AsBlendAble() != null || next.AsBlendAble() != null) && clip.GetType() == next.GetType())
                return true;
            return false;
        }

        public static IBlendAble AsBlendAble(this IClip clip) => clip as IBlendAble;
        public static void SetBlendIn(this IClip clip, float value)
        {
            var blend = clip.AsBlendAble();
            if (blend == null) return;
            blend.BlendIn = value;
        }
        public static void SetBlendOut(this IClip clip, float value)
        {
            var blend = clip.AsBlendAble();
            if (blend == null) return;
            blend.BlendOut = value;
        }
        public static void ValidBlend(this IBlendAble clip)
        {
            clip.SetBlendOut(Clamp(clip.BlendOut, 0, clip.Length - clip.BlendIn));
            clip.SetBlendIn(Clamp(clip.BlendIn, 0, clip.Length - clip.BlendOut));
        }

        public static bool CanScale(this IClip clip) => clip is IResizeAble;

        public static bool CanValidTime(this IClip clip) => CanValidTime(clip, clip.StartTime, clip.EndTime);


        public static bool CanValidTime(this IClip clip, float startTime, float endTime)
        {
            if (clip.Parent != null)
            {
                return CanValidTime(clip, clip.Parent, startTime, endTime);
            }

            return true;
        }

        public static bool CanValidTime(this IClip clip, ISegment parent, float startTime, float endTime)
        {
            var prevDirectable = clip.GetPreviousSibling(parent);
            var nextDirectable = clip.GetNextSibling(parent);

            var limitStartTime = 0f;
            var limitEndTime = float.MaxValue;

            if (prevDirectable != null)
            {
                limitStartTime = prevDirectable.EndTime;
                if (prevDirectable.CanCrossBlend(clip))
                {
                    limitStartTime = prevDirectable.StartTime;

                    //如果完全包含
                    if (startTime > limitStartTime && endTime < prevDirectable.EndTime)
                    {
                        return false;
                    }
                }
            }

            if (nextDirectable != null)
            {
                limitEndTime = nextDirectable.StartTime;
                if (clip.CanCrossBlend(nextDirectable))
                {
                    limitEndTime = nextDirectable.EndTime;
                }
            }

            if (limitStartTime - startTime > 0.0001f) //直接比大小存在精度问题
            {
                return false;
            }

            if (endTime - limitEndTime > 0.0001f)
            {
                return false;
            }

            return true;
        }




        public static IClip GetPreviousSibling(this IClip clip) => GetPreviousSibling(clip, clip.Parent);

        public static IClip GetPreviousSibling(this IClip clip, ISegment parent)
        {
            if (parent != null)
            {
                return parent.Children.LastOrDefault(d =>
                    d != clip && (d.StartTime < clip.StartTime)) as IClip;
            }

            return null;
        }

        public static IClip GetNextSibling(this IClip clip) => GetNextSibling(clip, clip.Parent);

        public static IClip GetNextSibling(this IClip clip, ISegment parent)
        {
            if (parent != null)
            {
                return parent.Children.FirstOrDefault(d =>
                    d != clip && d.StartTime > clip.StartTime) as IClip;
            }

            return null;
        }



        public static float GetWeight(this IBlendAble clip, float time) => GetWeight(clip, time, clip.BlendIn, clip.BlendOut);
        public static float GetWeight(this IClip clip, float time, float blendInOut) => GetWeight(clip, time, blendInOut, blendInOut);

        public static float GetWeight(this IClip clip, float time, float blendIn, float blendOut)
        {
            var length = clip.Length;
            time = clip.ToLocalTime(time);
            if (time < 0) return 0;
            if (time > length) return 0;
            if (time < blendIn) return time / blendIn;
            if (time > length - blendOut) return (length - time) / blendOut;
            return 1;
        }






    }
}