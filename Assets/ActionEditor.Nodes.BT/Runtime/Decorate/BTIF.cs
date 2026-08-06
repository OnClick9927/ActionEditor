using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("先执行第一个条件子节点，结果符合期望时才执行后续行为分支；可选择在行为运行期间持续重新检查条件。")]
    [Name("IF"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("IF")]
    public class BTIF : BTDecorateMuti
    {
        [Name("期望条件结果", "第一个条件子节点必须返回的布尔结果；不符合时当前节点直接失败且不会进入后续行为分支。")]
        public bool conditionTrue = true;
        [Name("每次更新检查", "开启后，行为分支运行期间每个逻辑 Tick 都重新求值条件；条件失效会中止正在运行的行为分支。")]
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
