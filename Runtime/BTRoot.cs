using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("行为树唯一的运行入口，每次 Update 都从这里转发到唯一子节点；缺少连接或存在多个根节点会在准备运行时直接报错。")]
    [System.Serializable, Name("根节点"), Attachable(typeof(BTTree)), Icon("Entry")]
    public class BTRoot : BTNode
    {
        [System.NonSerialized] private BTNode _child;
        public BTNode child => _child;

        internal void SetRuntimeChild(BTNode child) => _child = child;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (_child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            _child.Init(this, context);
        }
        protected sealed override void OnAbort(Blackboard blackboard) =>
            _child.Abort(blackboard);
        protected override State OnUpdate(Blackboard blackboard) =>
            _child.Update(blackboard);

        protected override int RuntimeChildCount => _child == null ? 0 : 1;
        protected override BTNode GetRuntimeChild(int index) => _child;
    }
}
