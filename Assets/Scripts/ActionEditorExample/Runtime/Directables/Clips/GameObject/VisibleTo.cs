using System;
using ActionEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("显示隐藏")]
    [Color(1, 90f / 255f, 90f / 255f)]
    [Attachable(typeof(ActionTrack))]
    public class VisibleTo : Clip
    {
        [Name("显示")] public bool visible = true;

        public override float Length
        {
            get => length;
            set => length = value;
        }

        //public override string Info => $"{(visible ? "显示" : "隐藏")}";

        public override bool IsValid => true;

        private ActionTrack Track => (ActionTrack)Parent;
    }
}