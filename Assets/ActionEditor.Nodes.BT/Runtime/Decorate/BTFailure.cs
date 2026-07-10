namespace ActionEditor.Nodes.BT
{
    [Name("Ê§°Ü"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Failure")]

    public class BTFailure : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Running)
                return State.Running;
            return State.Failure;
        }
    }
}
