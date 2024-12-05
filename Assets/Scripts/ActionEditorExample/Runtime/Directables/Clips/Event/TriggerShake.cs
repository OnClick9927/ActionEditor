using ActionEditor;

namespace ActionEditor
{
    [Name("触发震动")]
    [Color(1, 0, 0)]
    [Attachable(typeof(SignalTrack))]
    public class TriggerShake : ClipSignal
    {
        [Name("类型")]    
        public int shakeType;

        [Name("屏幕抖动时长")] 
        public float duration = 0.5f;

        [Name("屏幕抖动幅度")]
        public int range = 5;

        [Name("设备震动时长")]
        public float vibrationDuration = 0.5f;

        [Name("设备震动强度")]
        public int vibrationForce = EventShakeForceType.Default;

        //public override string Info
        //{
        //    get
        //    {
        //        var name = AttributesUtility.GetName(shakeType, typeof(EventShakeType));
        //        if (shakeType == EventShakeType.Screen || shakeType == EventShakeType.ScreenAndPhone)
        //        {
        //            return "震动\n" + name + duration + "s " + range+"px";
        //        }

        //        return "震动\n" + name;
        //    }
        //}

        public override bool IsValid => shakeType > 0;
    }
}