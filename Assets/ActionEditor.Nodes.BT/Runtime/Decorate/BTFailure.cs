namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("Ê§°Ü"), Attachable(typeof(BTTree)), Node("×°ÊÎ/Ê§°Ü")]

    public class BTFailure : BTDecorate
    {
        protected override State Decorate(State state)
        {
            if (state == State.Running)
                return State.Running;
            return State.Failure;
        }
    }
}
