using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    public abstract class BTDecorateMuti : BTDecorate
    {
        internal sealed override bool IsConditionDecorate()
        {
            if (_children == null) return false;
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                if (!(child is BTCondition))
                    return false;
            }
            return true;
        }
        [System.NonSerialized] private List<BTNode> _children;
        [System.NonSerialized] private IReadOnlyList<BTNode> _childView;
        protected IReadOnlyList<BTNode> children => _childView;
        protected int ChildCount => _children == null ? 0 : _children.Count;
        protected BTNode ChildAt(int index) => _children[index];

        internal void SetRuntimeChildren(List<BTNode> children)
        {
            _children = children;
            _childView = children?.AsReadOnly();
        }
        internal void ReplaceRuntimeChild(int index, BTNode child) =>
            _children[index] = child;

        protected void AbortRunningChildren()
        {
            for (int i = 0; i < _children.Count; ++i)
                _children[i].Abort();
        }

        protected sealed override void OnAbort()
        {
            if (_children == null) return;
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                child.Abort();
            }
        }
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (_children == null)
                throw new System.Exception($"{GetType()} {nameof(children)} is Null");
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                child.Init(blackboard, this, tree);
            }
        }
        protected virtual int GetStartIndex() { return 0; }
        protected override State OnUpdate()
        {
            State state = State.Inactive;
            for (int i = GetStartIndex(); i < _children.Count; i++)
            {
                var child = _children[i];
                var result = child.Update();
                var next = Decorate(i, ref state, result);
                if (!next) break;
            }
            return state;
        }
        protected abstract bool Decorate(int index, ref State src, State state);

        protected override int RuntimeChildCount =>
            _children == null ? 0 : _children.Count;
        protected override BTNode GetRuntimeChild(int index) => _children[index];
    }
}
