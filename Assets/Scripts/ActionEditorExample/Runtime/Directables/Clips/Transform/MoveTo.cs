using ActionEditor;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ActionEditor
{









    [Name("移动至")]
    [Attachable(typeof(ActionTrack))]
    public class MoveTo : Clip
    {
        [Name("运动补间")] public EaseType interpolation = EaseType.QuadInOut;

        [Name("位移终点")]
        public int moveType;

  
   

        //public override string Info => $"移动至:\n{AttributesUtility.GetName(moveType, typeof(MoveToType))}";

        public override float Length
        {
            get => length;
            set => length = value;
        }


        public override bool IsValid => true;

        private ActionTrack Track => (ActionTrack)Parent;
    }
}