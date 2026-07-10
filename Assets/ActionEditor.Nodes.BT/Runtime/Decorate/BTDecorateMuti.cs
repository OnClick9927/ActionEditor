using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    public abstract class BTDecorateMuti : BTDecorate
    {
        internal sealed override bool IsConditionDecorate()
        {
            if (children == null) return false;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (!(child is BTCondition))
                    return false;
            }
            return true;
        }
        public List<BTNode> children { get; internal set; }
        protected sealed override void OnAbort()
        {
            if (children == null) return;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                child.Abort();
            }
        }
        internal sealed override List<BTComposite> Init(Blackboard blackboard, BTNode parent, List<BTComposite> result)
        {
            base.Init(blackboard, parent, result);
            if (children == null)
                throw new System.Exception($"{GetType()} {nameof(children)} is Null");
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                child.Init(blackboard, this, result);
            }
            return result;
        }

        protected override State OnUpdate()
        {
            State state = State.Inactive;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var result = child.Update();
                var next = Decorate(ref state, result);
                if (!next) break;
            }
            return state;
        }
        protected abstract bool Decorate(ref State src, State state);

    }
}
