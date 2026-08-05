using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("或", "依次检查条件，任一条件成功时返回成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("OR")]
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
