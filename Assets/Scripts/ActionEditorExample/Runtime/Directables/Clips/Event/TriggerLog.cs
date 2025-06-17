using ActionEditor;

namespace ActionEditor
{
    [Name("打印日志")]
    [Attachable(typeof(SignalTrack))]
    public class TriggerLog : ClipSignal
    {
        [Name("打印内容")] public string log;



        [ReadOnly] public string test;
        //public override string Info => "打印\n" + log;
        public override bool IsValid => !string.IsNullOrEmpty(log);
    }
}