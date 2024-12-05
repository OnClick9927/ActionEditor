using System;
using System.Collections.Generic;
using ActionEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("旋转角度")]
    [Color(70f / 255f, 1, 140f / 255f)]
    [Attachable(typeof(ActionTrack))]
    public class RotateTo : Clip
    {

        [Name("运动曲线")] public EaseType interpolation = EaseType.QuadInOut;
        [Name("旋转角度")] public Vector3 targetRotation = Vector3.zero;
        public List<int> test;
        public Vector2[] test2;

        public int hh;
        public override float Length
        {
            get => length;
            set => length = value;
        }

        //public override string Info => $"旋转:\n{targetRotation}";

        public override bool IsValid => true;

        private ActionTrack Track => (ActionTrack)Parent;
    }
}