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

        protected override void OnAbort(Blackboard blackboard) =>
            _child.Abort(blackboard);
        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (_child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            _child.Init(this, context);
        }

        protected abstract State Decorate(Blackboard blackboard, State state);
        protected override State OnUpdate(Blackboard blackboard)
        {
            return Decorate(blackboard, _child.Update(blackboard));
        }

        protected override int RuntimeChildCount => _child == null ? 0 : 1;
        protected override BTNode GetRuntimeChild(int index) => _child;
    }
}
