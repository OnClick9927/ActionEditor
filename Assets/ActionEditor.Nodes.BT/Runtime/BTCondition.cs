using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [Icon("Conditional")]
    public abstract class BTCondition : BTNode
    {
        internal BTComposite composite { get; private set; }
        internal BTComposite lowerAbortComposite { get; private set; }

        protected sealed override void OnAbort() { }
        internal sealed override List<BTCondition> Init(Blackboard blackBord, BTNode parent, List<BTCondition> result)
        {

            base.Init(blackBord, parent, result);
            FindParentComposite();
            if (composite != null || composite.abortType != BTComposite.AbortType.None)
                result.Add(this);
            return result;
        }
        private void FindParentComposite()
        {
            var _node = parent;
            while (_node != null)
            {
                if (_node is BTComposite composite)
                {
                    if (this.composite == null)
                        this.composite = composite;
                    if (composite.abortType == BTComposite.AbortType.Both || composite.abortType == BTComposite.AbortType.LowerPriority)
                        lowerAbortComposite = composite;
                    else
                        break;
                }
                _node = _node.parent;
            }
        }

        protected abstract bool Condition();
        protected sealed override State OnUpdate()
        {
            return Condition() ? State.Success : State.Failure;
        }
    }
}
