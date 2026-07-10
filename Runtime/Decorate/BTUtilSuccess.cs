namespace ActionEditor.Nodes.BT
{
    [Name("直到成功"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("UtilSuccess")]
    public class BTUtilSuccess : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Success)
                return state;
            return State.Running;
        }
    }
}
