namespace ActionEditor.Nodes.BT
{
    [Name("”Î"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("And")]
    public class BTAnd : BTDecorateMuti
    {
        public bool GoNextIfFailure;
        protected override bool Decorate(ref State src, State state)
        {
            if (state == State.Failure)
            {
                src = State.Failure;
                if (!GoNextIfFailure)
                    return false;
            }
            if (src != State.Failure)
                src = state;

            return true;
        }
    }
}
