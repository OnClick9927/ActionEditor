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
        [System.NonSerialized] public List<BTNode> children;
        public int current { get; protected set; }
        protected override void OnStart()
        {
            base.OnStart();
            current = 0;
        }
        internal sealed override List<BTCondition> Init(Blackboard blackBord, BTNode parent, List<BTCondition> result)
        {
            base.Init(blackBord, parent, result);
            if (children == null)
                throw new System.Exception($"{GetType()} {nameof(children)} is Null");
            for (int i = 0; i < children.Count; i++)
            {
                children[i].Init(blackBord, this, result);
            }
            return result;
        }
        protected override void OnAbort()
        {
            this.current = 0;
            for (int i = 0; i < children.Count; i++)
                children[i].Abort();
        }
    }
}
