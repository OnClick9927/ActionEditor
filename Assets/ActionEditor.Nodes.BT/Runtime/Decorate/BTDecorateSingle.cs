namespace ActionEditor.Nodes.BT
{
    public abstract class BTDecorateSingle : BTDecorate
    {
        internal sealed override bool IsConditionDecorate()
        {
            return _child is BTCondition;
        }
        [System.NonSerialized] private BTNode _child;
        public BTNode child => _child;

        internal void SetRuntimeChild(BTNode child) => _child = child;

        protected override void OnAbort() => _child.Abort();
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (_child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            _child.Init(blackboard, this, tree);
        }

        protected abstract State Decorate(State state);
        protected override State OnUpdate()
        {
            return Decorate(_child.Update());
        }

        protected override int RuntimeChildCount => _child == null ? 0 : 1;
        protected override BTNode GetRuntimeChild(int index) => _child;
    }
}
