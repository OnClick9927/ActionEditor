using ActionAttribute;
using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.Dialog
{
    [Name("≤Ω÷Ë")]
    public class StepData : DialogData
    {
        [NodePort(NodePortAttribute.Direction.Input, single = false), NonSerialized]
        public StepData IN;

        [NodePort(NodePortAttribute.Direction.Output, single = false, type = typeof(OptionData)), NonSerialized]
        public List<OptionData> options;

        [ReadOnly] public int id;
        [Name("∂‘ª∞")] public string comment;
    }

}
