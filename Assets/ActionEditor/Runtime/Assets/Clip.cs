using System;
using System.Collections.Generic;


namespace ActionEditor
{
    [Serializable]
    public abstract class Clip : DirectableBase, IClip
    {



        [Buffer] private float startTime;
        [Buffer] protected float length = 1f;

        public sealed override bool IsActive { get => Parent == null ? false : Parent.IsActive; set { } }
        public sealed override bool IsLocked { get => Parent == null ? false : Parent.IsLocked; set { } }
        public override float Length
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
                    startTime = IDirectableExtensions.Max(value, 0);
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
                    Length = IDirectableExtensions.Max(value - StartTime, 0);
                    this.AsBlendAble()?.ValidBlend();

                }
            }
        }

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