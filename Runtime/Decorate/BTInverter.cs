namespace ActionEditor.Nodes.BT
{
    [ Name("È¡·´"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate)]

    public class BTInverter : BTDecorate
    {
        protected override State Decorate(State state)
        {
            if (state == State.Success)
                return State.Failure;
            if (state == State.Failure)
                return State.Success;
            return state;
        }
    }
}
