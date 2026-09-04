using ActionAttribute;
using System;
namespace ActionEditor.Nodes.Dialog
{
    [Name("选项")]
    public class OptionData : DialogData
    {
        [NodePort(NodePortAttribute.Direction.Input), NonSerialized]
        public OptionData IN;
        [Name("选项文本")] public string label;
    }

}
