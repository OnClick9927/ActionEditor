namespace ActionEditor.Nodes.BT
{
    [ Name("÷ÿ∏¥"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate)]

    public class BTRepeat : BTDecorate
    {
        public bool restartOnSuccess = true;
        public bool restartOnFailure = true;
        protected override State Decorate(State state)
        {
            if (state == State.Running)
                return State.Running;
            if (state == State.Failure)
                if (restartOnFailure)
                    return State.Running;
                else
                    return State.Failure;
            if (state == State.Success)
                if (restartOnSuccess)
                    return State.Running;
                else
                    return State.Success;
            return State.Success;
        }
    }
}
