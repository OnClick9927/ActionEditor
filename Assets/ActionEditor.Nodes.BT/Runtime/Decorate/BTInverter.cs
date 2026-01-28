namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("取反"), Attachable(typeof(BTTree)), Node("装饰/取反")]

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
