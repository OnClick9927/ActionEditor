using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("加载并执行另一份同类型行为树资源，共享调用方传入的黑板；准备运行时会拒绝缺失、类型不符或形成循环引用的子树。")]
    [System.Serializable, Name("子树"), Attachable(typeof(BTTree)), Icon("sub")]
    public class BTSubTree : BTNode
    {
        [ReadOnly, Name("子树资源", "加载器用于定位子树资源的路径键；该路径由编辑器选择资源时写入，运行时只读取且必须能唯一加载目标树。")]
        public string path;
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

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            if (_tree == null)
                throw new System.Exception($"{GetType()} runtime tree is Null");
            if (_tree.root == null || _tree.root.child == null)
                throw new System.Exception("Invalid  SubTree");
            _tree.root.child.Init(parent, context);
        }
        protected sealed override void OnAbort(Blackboard blackboard)
        {
            _tree.root.child.Abort(blackboard);
        }
        protected override State OnUpdate(Blackboard blackboard)
        {
            return _tree.root.child.Update(blackboard);
        }
    }
}
