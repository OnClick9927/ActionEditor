using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("×ÓÊ÷"), Attachable(typeof(BTTree)), Icon("sub")]
    public class BTSubTree : BTNode
    {
        public string path;
        public BTTree tree { get; internal set; }
        internal override List<BTCondition> Init(Blackboard blackboard, BTNode parent, List<BTCondition> result)
        {
            base.Init(blackboard, parent, result);
            if (tree == null)
                throw new System.Exception($"{GetType()} {nameof(tree)} is Null");
            if (tree.root==null || tree.root.child==null)
                throw new System.Exception("Invalid  SubTree");
            return tree.root.child.Init(blackboard, this, result);
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
