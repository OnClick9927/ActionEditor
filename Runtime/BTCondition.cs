using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [Icon("Conditional")]
    public abstract class BTCondition : BTNode
    {
        protected sealed override void OnAbort() { }
        internal sealed override List<BTComposite> Init(Blackboard blackboard, BTNode parent, List<BTComposite> result)
        {
            return base.Init(blackboard, parent, result);
        }

        protected abstract bool Condition();
        protected sealed override State OnUpdate()
        {
            return Condition() ? State.Success : State.Failure;
        }
    }
}
