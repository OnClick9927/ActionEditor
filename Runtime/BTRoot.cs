namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("¸ù½Úµã"), Attachable(typeof(BTTree)), Icon("Entry")]
    public class BTRoot : BTNode
    {
        public BTNode child { get; internal set; }
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            child.Init(blackboard, this, tree);
        }
        protected sealed override void OnAbort() => child.Abort();
        protected override State OnUpdate() => child.Update();
    }
}
