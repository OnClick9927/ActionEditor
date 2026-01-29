namespace ActionEditor.Nodes.BT
{
    [Name("³É¹¦"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate)]

    public class BTSuccess : BTDecorate
    {
        protected override State Decorate(State state)
        {
            if (state == State.Running)
                return State.Running;
            return State.Success;
        }
    }
}
