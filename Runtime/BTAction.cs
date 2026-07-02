using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [Icon("Action")]
    public abstract class BTAction : BTNode
    {
        protected override void OnAbort()
        {
            
        }
        internal sealed override List<BTCondition> Init(Blackboard blackBord, BTNode parent, List<BTCondition> result)
        {
            return base.Init(blackBord,parent, result);
        }
    }
}
