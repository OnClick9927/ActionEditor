using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [Icon("Action")]
    public abstract class BTAction : BTNode
    {
        protected override void OnAbort()
        {
            
        }
        internal sealed override List<BTComposite> Init(Blackboard blackBord, BTNode parent, List<BTComposite> result)
        {
            return base.Init(blackBord,parent, result);
        }
    }
}
