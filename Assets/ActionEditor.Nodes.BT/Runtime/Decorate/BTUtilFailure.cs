namespace ActionEditor.Nodes.BT
{
    [Name("Ö±µ½Ê§°Ü"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("UtilFailure")]
    public class BTUtilFailure : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Failure)
                return state;
            return State.Running;
        }
    }
}
