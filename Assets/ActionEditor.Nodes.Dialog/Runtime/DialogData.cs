using ActionAttribute;
using System;
namespace ActionEditor.Nodes.Dialog
{
    [Attachable(typeof(DialogAsset))]
    public abstract class DialogData : NodeData
    {
        [NodePort(NodePortAttribute.Direction.Output), NonSerialized]
        public StepData step;
    }

}
