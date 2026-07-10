using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    public abstract class BTDecorateSingle : BTDecorate
    {
        internal sealed override bool IsConditionDecorate()
        {
            return child is BTCondition;
        }
        public BTNode child { get; internal set; }
        protected sealed override void OnAbort() => child.Abort();
        internal sealed override List<BTComposite> Init(Blackboard blackboard, BTNode parent, List<BTComposite> result)
        {
            base.Init(blackboard, parent, result);
            if (child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            return child.Init(blackboard, this, result);
        }

        protected abstract State Decorate(State state);
        protected sealed override State OnUpdate()
        {
            return Decorate(child.Update());
        }
    }
}
