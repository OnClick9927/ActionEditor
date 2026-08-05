using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("子树", "引用并执行另一棵行为子树。"), Attachable(typeof(BTTree)), Icon("sub")]
    public class BTSubTree : BTNode
    {
        [ReadOnly]public string path;
        [System.NonSerialized] private BTNode _runtimeNode;
        [System.NonSerialized] private BTTree _tree;

        public BTNode runtimeNode => _runtimeNode;
        public BTTree tree => _tree;

        internal void SetRuntimeTree(BTTree tree) => _tree = tree;
        internal void SetRuntimeNode(BTNode node) => _runtimeNode = node;
        internal void ResetRuntimeData()
        {
            _runtimeNode = null;
            _tree = null;
        }
        
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            //base.Init(blackboard, parent, tree);
            if (_tree == null)
                throw new System.Exception($"{GetType()} runtime tree is Null");
            if (_tree.root == null || _tree.root.child == null)
                throw new System.Exception("Invalid  SubTree");
            _tree.root.child.Init(blackboard, parent, tree);
        }
        protected sealed override void OnAbort()
        {
            _tree.root.child.Abort();
        }
        protected override State OnUpdate()
        {
            return _tree.root.child.Update();
        }
    }
}
