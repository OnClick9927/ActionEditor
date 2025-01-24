using System;
using System.Linq;
using UnityEngine;

namespace ActionEditor
{
    public static class IDirectableExtensions
    {

        //public static float GetLength(this IDirectable directable)
        //{
        //    return directable.EndTime - directable.StartTime;
        //}

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
        public static float ToLocalTime(this IDirectable directable, float time)
        {
            return Clamp(time - directable.StartTime, 0, directable.Length);
        }


        public static float ToLocalTimeUnclamped(this IDirectable directable, float time)
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

        public static IBlendAble AsBlendAble(this IClip clip)
        {
            return clip as IBlendAble;
        }
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
            clip.SetBlendOut(Mathf.Clamp(clip.BlendOut, 0, clip.Length - clip.BlendIn));
            clip.SetBlendIn(Mathf.Clamp(clip.BlendIn, 0, clip.Length - clip.BlendOut));
        }

        public static bool CanScale(this IClip clip)
        {
            return clip is IResizeAble;
            //var lengthProp = clip.GetType().GetProperty(nameof(IClip.Length), BindingFlags.Instance | BindingFlags.Public);
            //return lengthProp != null && lengthProp.CanWrite
            //   && lengthProp.DeclaringType != typeof(Clip);

        }


        /// <summary>
        /// 当前开始时间是否可用
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static bool CanValidTime(this IClip clip)
        {
            return CanValidTime(clip, clip.StartTime, clip.EndTime);
        }

        /// <summary>
        /// 当前开始时间是否可用
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static bool CanValidTime(this IClip clip, float startTime, float endTime)
        {
            if (clip.Parent != null)
            {
                return CanValidTime(clip, clip.Parent, startTime, endTime);
            }

            return true;
        }

        /// <summary>
        /// 当前开始时间是否可用
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="parent"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static bool CanValidTime(this IClip clip, IDirectable parent, float startTime, float endTime)
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



        #region 切片获取

        public static T GetPreviousSibling<T>(this IClip clip) where T : IClip
        {
            return (T)GetPreviousSibling(clip, clip.Parent);
        }

        public static IClip GetPreviousSibling(this IClip clip)
        {
            return GetPreviousSibling(clip, clip.Parent);
        }

        public static IClip GetPreviousSibling(this IClip clip, IDirectable parent)
        {
            if (parent != null)
            {
                return parent.Children.LastOrDefault(d =>
                    d != clip && (d.StartTime < clip.StartTime)) as IClip;
            }

            return null;
        }

        /// <summary>
        /// 返回父对象的下个同级
        /// </summary>
        /// <param name="clip"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetNextSibling<T>(this IClip clip) where T : IClip
        {
            return (T)GetNextSibling(clip, clip.Parent);
        }

        /// <summary>
        /// 返回父对象的下个同级
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static IClip GetNextSibling(this IClip clip)
        {
            return GetNextSibling(clip, clip.Parent);
        }

        /// <summary>
        /// 返回指定轨道的下个子对象
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static IClip GetNextSibling(this IClip clip, IDirectable parent)
        {
            if (parent != null)
            {
                return parent.Children.FirstOrDefault(d =>
                    d != clip && d.StartTime > clip.StartTime) as IClip;
            }

            return null;
        }

        #endregion

        #region 混合权重

        /// <summary>
        /// 根据其混合特性在指定当地时间的权重。
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static float GetWeight(this IBlendAble clip, float time)
        {
            return GetWeight(clip, time, clip.BlendIn, clip.BlendOut);
        }

        /// <summary>
        /// 基于所提供的覆盖混合入/出属性在指定本地时间的权重
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="time"></param>
        /// <param name="blendInOut"></param>
        /// <returns></returns>
        public static float GetWeight(this IClip clip, float time, float blendInOut)
        {
            return GetWeight(clip, time, blendInOut, blendInOut);
        }

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

        #endregion

        #region 循环长度

        /// <summary>
        /// 返回剪辑的上一个循环长度
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        //public static float GetPreviousLoopLocalTime(this ISubClipContainable clip)
        //{
        //    var clipLength = clip.GetLength();
        //    var loopLength = clip.SubClipLength / clip.SubClipSpeed;
        //    if (clipLength > loopLength)
        //    {
        //        var mod = (clipLength - clip.SubClipOffset) % loopLength;
        //        var aproxZero = Math.Abs(mod) < 0.01f;
        //        return clipLength - (aproxZero ? loopLength : mod);
        //    }

        //    return clipLength;
        //}

        /// <summary>
        /// 返回剪辑的下一个循环长度
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        //public static float GetNextLoopLocalTime(this ISubClipContainable clip)
        //{
        //    var clipLength = clip.GetLength();
        //    var loopLength = clip.SubClipLength / clip.SubClipSpeed;
        //    var mod = (clipLength - clip.SubClipOffset) % loopLength;
        //    var aproxZero = Math.Abs(mod) < 0.01f || Math.Abs(loopLength - mod) < 0.01f;
        //    return clipLength + (aproxZero ? loopLength : loopLength - mod);
        //}

        #endregion



    }
}