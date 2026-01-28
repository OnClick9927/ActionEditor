using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    public abstract class BTCondition : BTNode
    {
        protected sealed override void OnAbort() { }
        internal sealed override List<BTCondition> Init(Blackboard blackBord, BTNode parent, List<BTCondition> result)
        {
            base.Init(blackBord, parent, result);
            result.Add(this);
            return result;
        }
     
        protected abstract bool Condition();
        protected sealed override State OnUpdate()
        {
            return Condition() ? State.Success : State.Failure;
        }
    }
}
