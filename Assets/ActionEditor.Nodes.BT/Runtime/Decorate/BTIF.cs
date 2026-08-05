using System;
using System.Collections.Generic;
using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("IF", "根据第一个条件子节点的结果决定是否执行后续子节点。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("IF")]
    public class BTIF : BTDecorateMuti
    {
        public bool conditionTrue = true;
        public bool CheckEachUpdate = true;
        [NonSerialized] private bool eveFirst;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (ChildCount != 2)
                throw new System.Exception("BTIF children must be two");
            var first = ChildAt(0);
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

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(eveFirst ? 1 : 0);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value != 0 && value != 1)
                throw new ArgumentException("Invalid IF runtime status",
                    nameof(values));
            eveFirst = value != 0;
        }
    }
}
