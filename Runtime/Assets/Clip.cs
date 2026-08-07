using ActionBuffer;
using System;
using ActionAttribute;
using System.Collections.Generic;


namespace ActionEditor
{
    [Serializable]
    [TypeInfoBox("时间轴片段，表示指定起始时间和持续时长内执行的内容。")]
    public abstract class Clip : SegmentBase, IClip
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

        public sealed override IEnumerable<ISegment> Children => null;


        public sealed override float StartTime
        {
            get => startTime;
            set
            {
                if (Math.Abs(startTime - value) > 0.0001f)
                {
                    startTime = SegmentExtensions.Max(value, 0);
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
                    Length = SegmentExtensions.Max(value - StartTime, 0);
                    this.AsBlendAble()?.ValidBlend();

                }
            }
        }

    }


    [Serializable]
    [TypeInfoBox("时间轴信号，在单个时间点触发，不占用持续时长。")]
    public abstract class ClipSignal : Clip
    {
        public override float Length
        {
            get => 0;
            //set => TimeCache();
        }
    }

}
