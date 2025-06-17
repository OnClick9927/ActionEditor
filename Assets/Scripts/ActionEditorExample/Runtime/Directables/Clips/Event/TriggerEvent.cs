using ActionEditor;

namespace ActionEditor
{
    [Name("触发事件")]
    [Attachable(typeof(SignalTrack))]
    public class TriggerEvent : ClipSignal
    {
        [Name("事件名称")] 
        public int eventName;

        [Name("结算方式")]  
        public int calculateType = 0;

        [Name("拆开份数")]
        public int calculateArgs = 0;

        [Name("必杀概率")]
        public int kill;

        //public override string Info => "事件\n" + AttributesUtility.GetName(eventName, typeof(EventNames));
        public override bool IsValid => eventName > 0;
    }
}