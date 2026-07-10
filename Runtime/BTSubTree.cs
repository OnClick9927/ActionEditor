namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("×ÓÊ÷"), Attachable(typeof(BTTree)), Icon("sub")]
    public class BTSubTree : BTNode
    {
        public string path;
        public BTTree tree { get; internal set; }
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (tree == null)
                throw new System.Exception($"{GetType()} {nameof(tree)} is Null");
            if (tree.root == null || tree.root.child == null)
                throw new System.Exception("Invalid  SubTree");
            tree.root.child.Init(blackboard, this, tree);
        }
        protected sealed override void OnAbort()
        {
            tree.root.child.Abort();
        }
        protected override State OnUpdate()
        {
            return tree.root.child.Update();
        }
    }
}
