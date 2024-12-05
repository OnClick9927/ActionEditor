using System;
using System.Collections.Generic;
//using FullSerializer;
using UnityEngine;

namespace ActionEditor
{
    [Serializable]
    public abstract class Clip : DirectableBase, IClip
    {
        [SerializeField] private float startTime;
        [SerializeField][HideInInspector] protected float length = 1f;


        [Name("片段长度")]
        public virtual float Length
        {
            get => length;
            set
            {
                length = value;
            }
        }

        public abstract bool IsValid { get; }

        public sealed override IEnumerable<IDirectable> Children => null;


        [Name("开始时间")]
        public sealed override float StartTime
        {
            get => startTime;
            set
            {
                if (Math.Abs(startTime - value) > 0.0001f)
                {
                    startTime = Mathf.Max(value, 0);
                }
            }
        }

        [Name("结束时间")]
        public sealed override float EndTime
        {
            get => StartTime + Length;
            set
            {
                if (Math.Abs(StartTime + Length - value) > 0.0001f)
                {
                    Length = Mathf.Max(value - StartTime, 0);
                    BlendOut = Mathf.Clamp(BlendOut, 0, Length - BlendIn);
                    BlendIn = Mathf.Clamp(BlendIn, 0, Length - BlendOut);
                }
            }
        }

        public sealed override bool IsActive
        {
            get => Parent?.IsActive ?? false;
            set { }
        }

        public sealed override bool IsCollapsed
        {
            get { return Parent != null && Parent.IsCollapsed; }
            set { }
        }

        public sealed override bool IsLocked
        {
            get { return Parent != null && Parent.IsLocked; }
            set { }
        }

        public virtual float BlendIn
        {
            get => 0;
            set { }
        }

        public virtual float BlendOut
        {
            get => 0;
            set { }
        }
    
        public virtual bool CanCrossBlend => false;


        public Clip GetNextClip() => this.GetNextSibling<Clip>();

        public float GetClipWeight(float time) => GetClipWeight(time, BlendIn, BlendOut);

        public float GetClipWeight(float time, float blendInOut) => GetClipWeight(time, blendInOut, blendInOut);

        public float GetClipWeight(float time, float blendIn, float blendOut) => this.GetWeight(time, blendIn, blendOut);


        public void TryMatchSubClipLength()
        {
            if (this is ISubClipContainable)
                Length = ((ISubClipContainable)this).SubClipLength / ((ISubClipContainable)this).SubClipSpeed;
        }

        public void TryMatchPreviousSubClipLoop()
        {
            if (this is ISubClipContainable) Length = (this as ISubClipContainable).GetPreviousLoopLocalTime();
        }

        public void TryMatchNexSubClipLoop()
        {
            if (this is ISubClipContainable)
            {
                var targetLength = (this as ISubClipContainable).GetNextLoopLocalTime();
                var nextClip = GetNextClip();
                if (nextClip == null || StartTime + targetLength <= nextClip.StartTime) Length = targetLength;
            }
        }
    }


    [Serializable]
    public abstract class ClipSignal : Clip
    {
        public sealed override float Length
        {
            get => 0;
            //set => TimeCache();
        }
    }

}