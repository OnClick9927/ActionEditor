using ActionEditor;

namespace ActionEditor
{
    public partial class EventNames
    {
        public const int None = 0;
        [Name("测试事件")] public const int Test = 1;
        [Name("触发击中")] public const int Hit = 2;
        [Name("必杀检测")] public const int Kill = 3;
    }

    public partial class EventHitTypes
    {
       [Name("正常结算")] public const int Def = 0;
        [Name("拆分结算")] public const int Gap = 1;
        [Name("不结算")] public const int Fake = 2;
    }

    public partial class EventShakeType
    {
        public const int None = 0;
        [Name("屏幕")] public const int Screen = 1;
        [Name("手机")] public const int Phone = 2;
        [Name("手机和屏幕")] public const int ScreenAndPhone = 3;
    }

    public partial class EventShakeForceType
    {
        /// <summary>
        /// 选中 
        /// </summary>
        [Name("Selection")] public const int Selection = 0;

        /// <summary>
        /// 常规震动
        /// </summary>
        [Name("Vibrate")] public const int Vibrate = 7;

        /// <summary>
        /// Unity自带
        /// </summary>
        [Name("Default")] public const int Default = 8;
    }

    public class TrackLayer
    {
        [Name("底层")] public const int Bottom = 0;
        [Name("角色层")] public const int Mid = 1;
        [Name("最顶层")] public const int TopHighest = 2;
        [Name("随便什么层")] public const int Custom_NoSet = 3;
    }


    public partial class MoveToType
    {
        [Name("None")]  public const int None = 0;
        [Name("目标面前")] public const int Target = 1;
        [Name("初始站位")] public const int OriginalPosition = 2;
    }
}