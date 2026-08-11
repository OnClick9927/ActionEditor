using System;
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
        private bool HasEvaluatedFirst(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0) != 0;
        private void SetEvaluatedFirst(Blackboard blackboard, bool value) =>
            SetRuntimeData(blackboard, 0, value ? 1 : 0);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => 1;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (ChildCount != 2)
                throw new System.Exception("BTIF children must be two");
            var first = ChildAt(0);
            if (!(first is BTCondition))
                throw new System.Exception("BTIF first child must be BTCondition");
        }
        protected override void OnStart(Blackboard blackboard)
        {
            base.OnStart(blackboard);
            SetEvaluatedFirst(blackboard, false);
        }

        protected override int GetStartIndex(Blackboard blackboard)
        {
            if (CheckEachUpdate) return 0;
            return HasEvaluatedFirst(blackboard) ? 1 : 0;
        }
        protected override bool Decorate(Blackboard blackboard, int index,
            ref State src, State state)
        {
            if (index == 0)
            {
                SetEvaluatedFirst(blackboard, true);
                State target = conditionTrue ? State.Success : State.Failure;
                if (state != target)
                {
                    src = State.Failure;
                    AbortRunningChildren(blackboard);
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
