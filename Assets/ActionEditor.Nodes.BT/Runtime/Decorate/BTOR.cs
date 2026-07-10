namespace ActionEditor.Nodes.BT
{
    [Name("»ò"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("OR")]
    public class BTOR : BTDecorateMuti
    {
        public bool GoNextIfSuccess;

        protected override bool Decorate(ref State src, State state)
        {
            if (state == State.Success)
            {
                src = State.Success;
                if (!GoNextIfSuccess)
                    return false;
            }
            if (src != State.Success)
                src = state;
            return true;
        }
    }
}
