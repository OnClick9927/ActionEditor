namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("÷ÿ∏¥"), Attachable(typeof(BTTree)), Node("◊∞ Œ/÷ÿ∏¥")]

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
