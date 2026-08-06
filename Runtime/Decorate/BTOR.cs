using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("或", "按固定顺序求值条件子节点；遇到成功立即成功，遇到运行中立即返回运行中，仅当全部条件失败时才失败。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("OR")]
    public class BTOR : BTDecorateMuti
    {
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            for (int i = 0; i < ChildCount; i++)
            {
                var child = ChildAt(i);
                if (!(child is BTCondition))
                {
                    throw new System.Exception("BTOR children must be BTCondition");
                }
            }
        }
        protected override bool Decorate(int index, ref State src, State state)
        {
            if (state == State.Success)
            {
                src = State.Success;
                return false;
            }
            if (src != state)
                src = state;
            return true;
        }
    }
}
