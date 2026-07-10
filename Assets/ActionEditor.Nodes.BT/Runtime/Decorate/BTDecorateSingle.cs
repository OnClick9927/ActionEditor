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
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            child.Init(blackboard, this, tree);
        }

        protected abstract State Decorate(State state);
        protected sealed override State OnUpdate()
        {
            return Decorate(child.Update());
        }
    }
}
