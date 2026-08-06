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


        private BTComposite CompositeParent;
        private BTNode AutoAbortCondition;
        private bool abortLower;
        private bool abortSelf;
        private BTNode FindAutoAbortCondition()
        {
            for (int i = 0; i < _children.Count; i++)
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
        internal void TryAutoAbort()
        {

            if (abortLower
                && state != State.Running
                && CompositeParent.state == State.Running
                && AutoAbortCondition.Update() == State.Success)
                CompositeParent.Abort();
            if (abortSelf
                && state == State.Running
                && AutoAbortCondition.Update() == State.Success)
                Abort();
        }
        internal sealed override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
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
                
                tree.AddSpecialNode(this);
            }

            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Init(blackboard, this, tree);
            }
            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        protected override void OnAbort()
        {
            AbortRunningChildren();
        }

        protected override int RuntimeChildCount =>
            _children == null ? 0 : _children.Count;
        protected override BTNode GetRuntimeChild(int index) => _children[index];
    }
}
