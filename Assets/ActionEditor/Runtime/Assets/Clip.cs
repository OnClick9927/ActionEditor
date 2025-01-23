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

        public sealed override float EndTime
        {
            get => StartTime + Length;
            set
            {
                if (Math.Abs(StartTime + Length - value) > 0.0001f)
                {
                    Length = Mathf.Max(value - StartTime, 0);
                    this.AsBlendAble()?.ValidBlend();

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

        //public virtual float BlendIn
        //{
        //    get => 0;
        //    set { }
        //}

        //public virtual float BlendOut
        //{
        //    get => 0;
        //    set { }
        //}

        //public virtual bool CanCrossBlend => false;


        public Clip GetNextClip() => this.GetNextSibling<Clip>();

    }


    [Serializable]
    public abstract class ClipSignal : Clip
    {
        public override float Length
        {
            get => 0;
            //set => TimeCache();
        }
    }

}