using ActionEditor;

namespace ActionEditor
{
    [Name("触发事件")]
    [Description("触发一个事件")]
    [Color(1, 0, 0)]
    [Attachable(typeof(SignalTrack))]
    public class TriggerEvent : ClipSignal
    {
        [MenuName("事件名称")] 
        public int eventName;

        [MenuName("结算方式")]  
        public int calculateType = 0;

        [MenuName("拆开份数")]
        public int calculateArgs = 0;

        [MenuName("必杀概率")]
        public int kill;

        //public override string Info => "事件\n" + AttributesUtility.GetMenuName(eventName, typeof(EventNames));
        public override bool IsValid => eventName > 0;
    }
}