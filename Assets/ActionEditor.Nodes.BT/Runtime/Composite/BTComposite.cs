using System.Collections.Generic;
using ActionAttribute;
namespace ActionEditor.Nodes.BT
{
    public abstract class BTComposite : BTNode
    {
        public enum AbortType
        {
            None,
            Self,
            LowerPriority,
            Both
        }

        [Name("中止方式", "指定条件节点结果变化时允许中止当前分支、自身以下分支或低优先级分支的范围；只影响正在运行的节点。")]
        public AbortType abortType;
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


        private BTComposite CompositeParent;
        private BTNode AutoAbortCondition;
        private bool abortLower;
        private bool abortSelf;
        private BTNode FindAutoAbortCondition()
        {
            for (int i = 0; i < _children.Length; i++)
            {
                var child = _children[i];
                if (child is BTCondition)
                {
                    AutoAbortCondition = child;
                    break;
                }
                else if (child is BTDecorate decorate)
                {
                    if (decorate.IsConditionDecorate())
                    {
                        AutoAbortCondition = child;
                        break;
                    }
                }
            }
            return AutoAbortCondition;
        }
        internal void TryAutoAbort(Blackboard blackboard)
        {
            State state = GetStateFast(blackboard);
            if (state == State.Running)
            {
                if (abortSelf &&
                    AutoAbortCondition.Update(blackboard) == State.Failure)
                    Abort(blackboard);
                return;
            }

            if (abortLower &&
                CompositeParent.GetStateFast(blackboard) == State.Running &&
                AutoAbortCondition.Update(blackboard) == State.Success)
                CompositeParent.Abort(blackboard);
        }
        internal bool HasAutoAbort => abortLower || abortSelf;

        internal sealed override void Init(BTNode parent,
            BTPrepareContext context)
        {
            base.Init(parent, context);
            if (_children == null)
                throw new System.Exception($"{GetType()} {nameof(children)} is Null");
            CompositeParent = null;
            AutoAbortCondition = null;
            abortLower = abortType == AbortType.Both || abortType == AbortType.LowerPriority;
            abortSelf = abortType == AbortType.Both || abortType == AbortType.Self;

            if (abortLower || abortSelf)
            {
                var condition = FindAutoAbortCondition();
                if (condition == null)
                    throw new System.Exception($" {this.abortType} need {nameof(AutoAbortCondition)}");
                if (abortLower)
                {
                    CompositeParent = FindParentComposite();
                    if (CompositeParent == null)
                        throw new System.Exception($" {this.abortType} need {nameof(CompositeParent)}");
                }
            }

            for (int i = 0; i < _children.Length; i++)
            {
                _children[i].Init(this, context);
            }
            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        protected override void OnAbort(Blackboard blackboard)
        {
            AbortRunningChildren(blackboard);
        }

        protected override int RuntimeChildCount =>
            _children == null ? 0 : _children.Length;
        protected override BTNode GetRuntimeChild(int index) => _children[index];
    }
}
