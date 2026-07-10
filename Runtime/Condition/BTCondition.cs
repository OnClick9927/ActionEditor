using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [Icon("Conditional")]
    public abstract class BTCondition : BTNode
    {
        protected sealed override void OnAbort() { }
        protected abstract bool Condition();
        protected sealed override State OnUpdate()
        {
            return Condition() ? State.Success : State.Failure;
        }
    }
}
