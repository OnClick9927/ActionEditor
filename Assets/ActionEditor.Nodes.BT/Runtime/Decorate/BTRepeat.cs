namespace ActionEditor.Nodes.BT
{
    [ Name("÷ÿ∏¥"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("Repeater")]

    public class BTRepeat : BTDecorateSingle
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
