namespace ActionEditor.Nodes.BT
{
    [Name("”Î"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("And")]
    public class BTAnd : BTDecorateMuti
    {
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
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
