using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    public abstract class BTDecorate : BTNode
    {
        [System.NonSerialized] public BTNode child;
        protected sealed override void OnAbort() => child.Abort();
        internal sealed override List<BTCondition> Init(Blackboard blackBord, BTNode parent, List<BTCondition> result)
        {
            base.Init(blackBord, parent, result);
            if (child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            return child.Init(blackBord, this, result);
        }

        protected abstract State Decorate(State state);
        protected sealed override State OnUpdate()
        {
            return Decorate(child.Update());
        }
    }
}
