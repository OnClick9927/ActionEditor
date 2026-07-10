using static ActionEditor.Nodes.BT.BTComposite;
namespace ActionEditor.Nodes.BT
{
    [Name("ÖÐ¶Ï"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Interrupt")]

    public class BTInterrupt : BTDecorateSingle
    {

        public AbortType abortType;
        bool abortLower;
        bool abortSelf;
        private BTComposite CompositeParent;
        [ReadOnly]public string flag;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {

            base.Init(blackboard, parent, tree);
            abortLower = abortType == AbortType.Both || abortType == AbortType.LowerPriority;
            abortSelf = abortType == AbortType.Both || abortType == AbortType.Self;
            if (abortLower)
            {
                CompositeParent = FindParentComposite();
                if (CompositeParent == null)
                    throw new System.Exception($" {this.abortType} need {nameof(CompositeParent)}");
            }
            if (abortSelf || abortLower)
                tree.AddAbortNode(this);
        }
        public void Interrupt()
        {
            if (abortLower)
                CompositeParent.Abort();
            if (abortSelf && state == State.Running)
                Abort();
        }
        protected override State Decorate(State state)
        {
            return state;
        }
    }
}
