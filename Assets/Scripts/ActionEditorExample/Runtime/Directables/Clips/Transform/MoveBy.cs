using System;
using ActionEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("移动")]
    [Attachable(typeof(ActionTrack))]
    public class MoveBy : Clip
    {

        [Name("运动曲线")] public AnimationCurve curve;

        [Name("运动补间")] public EaseType interpolation = EaseType.QuadInOut;

        [Name("移动量")] public Vector3 move;

        //public override string Info => $"位移:\n{move}";

        public override float Length
        {
            get => length;
            set => length = value;
        }


        public override bool IsValid => true;

        private ActionTrack Track => (ActionTrack)Parent;
    }
}