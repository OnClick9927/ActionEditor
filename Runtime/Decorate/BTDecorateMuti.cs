using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    public abstract class BTDecorateMuti : BTDecorate
    {
        internal sealed override bool IsConditionDecorate()
        {
            if (_children == null) return false;
            for (int i = 0; i < _children.Length; i++)
            {
                var child = _children[i];
                if (!(child is BTCondition))
                    return false;
            }
            return true;
        }
        [System.NonSerialized] private BTNode[] _children;
        [System.NonSerialized] private IReadOnlyList<BTNode> _childView;
        protected IReadOnlyList<BTNode> children => _childView;
        protected int ChildCount => _children == null ? 0 : _children.Length;
        protected BTNode ChildAt(int index) => _children[index];

        internal void SetRuntimeChildren(List<BTNode> children)
        {
            _children = children?.ToArray();
            _childView = _children == null
                ? null
                : System.Array.AsReadOnly(_children);
        }
        internal void ReplaceRuntimeChild(int index, BTNode child) =>
            _children[index] = child;

        protected void AbortRunningChildren(Blackboard blackboard)
        {
            for (int i = 0; i < _children.Length; ++i)
                _children[i].Abort(blackboard);
        }

        protected sealed override void OnAbort(Blackboard blackboard)
        {
            if (_children == null) return;
            for (int i = 0; i < _children.Length; i++)
            {
                var child = _children[i];
                child.Abort(blackboard);
            }
        }
        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (_children == null)
                throw new System.Exception($"{GetType()} {nameof(children)} is Null");
            for (int i = 0; i < _children.Length; i++)
            {
                var child = _children[i];
                child.Init(this, context);
            }
        }
        protected virtual int GetStartIndex(Blackboard blackboard) { return 0; }
        protected override State OnUpdate(Blackboard blackboard)
        {
            State state = State.Inactive;
            for (int i = GetStartIndex(blackboard); i < _children.Length; i++)
            {
                var child = _children[i];
                var result = child.Update(blackboard);
                var next = Decorate(blackboard, i, ref state, result);
                if (!next) break;
            }
            return state;
        }
        protected abstract bool Decorate(Blackboard blackboard, int index,
            ref State src, State state);

        protected override int RuntimeChildCount =>
            _children == null ? 0 : _children.Length;
        protected override BTNode GetRuntimeChild(int index) => _children[index];
    }
}
