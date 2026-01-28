namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("成功"), Attachable(typeof(BTTree)), Node("装饰/成功")]

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
