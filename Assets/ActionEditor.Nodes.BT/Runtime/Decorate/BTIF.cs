using System;

namespace ActionEditor.Nodes.BT
{
    [Name("IF"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("IF")]
    public class BTIF : BTDecorateMuti
    {
        public bool conditionTrue = true;
        public bool CheckEachUpdate = true;
        [NonSerialized] private bool eveFirst;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (children.Count != 2)
                throw new System.Exception("BTIF children must be two");
            var first = children[0];
            if (!(first is BTCondition))
                throw new System.Exception("BTIF first child must be BTCondition");
        }
        protected override void OnStart()
        {
            base.OnStart();
            eveFirst = false;
        }

        protected override int GetStartIndex()
        {
            if (CheckEachUpdate) return 0;
            return eveFirst ? 1 : 0;
        }
        protected override bool Decorate(int index, ref State src, State state)
        {
            if (index == 0)
            {
                eveFirst = true;
                State target = conditionTrue ? State.Success : State.Failure;
                if (state != target)
                {
                    src = State.Failure;
                    AbortRunningChildren();
                    return false;
                }
                else
                {
                    src = State.Success;
                    return true;
                }
            }
            else
                src = state;
            return true;
        }
    }
}
