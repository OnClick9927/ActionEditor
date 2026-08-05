using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("与", "依次检查全部条件，只有全部成功时才返回成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("And")]
    public class BTAnd : BTDecorateMuti
    {
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            for (int i = 0; i < ChildCount; i++)
            {
                var child = ChildAt(i);
                if (!(child is BTCondition))
                {
                    throw new System.Exception("BTAnd children must be BTCondition");
                }
            }
        }
        protected override bool Decorate(int index, ref State src, State state)
        {
            if (state == State.Failure)
            {
                src = State.Failure;
                return false;
            }
            if (src != state)
                src = state;
            return true;
        }
    }
}
