using ActionEditor;
using System;
using UnityEngine;

namespace ActionEditor
{
    [Name("打印日志")]
    [Attachable(typeof(SignalTrack))]
    public class TriggerLog : ClipSignal
    {
        [Name("打印内容")] public string log;
        [Serializable]
        public class Test
        {
            [ReadOnly] public string test_readonly;
            [TextArea] public string a;
            public int[] array;
        }
        public Test test;
        //public string hh;
        //public override string Info => "打印\n" + log;
        public override bool IsValid => !string.IsNullOrEmpty(log);
    }
}