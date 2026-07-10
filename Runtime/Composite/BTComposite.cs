using System.Collections.Generic;
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

        public AbortType abortType;
        public List<BTNode> children { get; internal set; }
        protected void AbortRunningChildren()
        {
            for (int i = 0; i < children.Count; ++i)
                children[i].Abort();
        }

        private BTComposite FindParentComposite()
        {
            var _node = parent;
            while (_node != null)
            {
                if (_node is BTComposite composite)
                {
                    CompositeParent = composite;
                    break;
                }
                _node = _node.parent;
            }
            return CompositeParent;
        }
        private BTComposite CompositeParent;
        private BTNode AutoAbortCondition;
        private BTNode FindAutoAbortCondition()
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
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
            if (abortType == AbortType.Both
                   || abortType == AbortType.LowerPriority)
            {
                if (state != State.Running)
                {
                    if (AutoAbortCondition.Update() == State.Success)
                        CompositeParent.Abort();
                }

            }
            if (abortType == AbortType.Both
                || abortType == AbortType.Self)
            {
                if (state == State.Running)
                {
                    if (AutoAbortCondition.Update() == State.Success)
                        Abort();
                }
            }
        }
        internal sealed override List<BTComposite> Init(Blackboard blackboard, BTNode parent, List<BTComposite> result)
        {
            base.Init(blackboard, parent, result);
            if (children == null)
                throw new System.Exception($"{GetType()} {nameof(children)} is Null");
            if (this.abortType != AbortType.None)
            {
                var condition = FindAutoAbortCondition();
                if (condition == null)
                    throw new System.Exception($" {this.abortType} need {nameof(AutoAbortCondition)}");
                if (this.abortType == AbortType.LowerPriority || this.abortType == AbortType.Both)
                {
                    var _result = FindParentComposite();
                    if (_result == null)
                        throw new System.Exception($" {this.abortType} need {nameof(CompositeParent)}");
                }
                result.Add(this);
            }

            for (int i = 0; i < children.Count; i++)
            {
                children[i].Init(blackboard, this, result);
            }
            return result;
        }
        protected override void OnAbort()
        {
            AbortRunningChildren();
        }
    }
}
