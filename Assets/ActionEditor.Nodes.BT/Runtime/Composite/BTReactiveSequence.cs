using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("每个 Tick 都从第一个前置子节点重新求值；任一前置项不再成功时，会中止此前运行的后续分支并返回对应状态。")]
    [Name("响应序列"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ReactiveSequence")]
    public sealed class BTReactiveSequence : BTComposite
    {
        private int GetRunningIndex(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetRunningIndex(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetInitialRuntimeData(int index) => -1;
        protected override int GetMinRuntimeData(int index) => -1;
        protected override int GetMaxRuntimeData(int index) => ChildCount - 1;

        protected override void OnStart(Blackboard blackboard)
        {
            if (GetRunningIndex(blackboard) != -1)
                SetRunningIndex(blackboard, -1);
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            for (int i = 0; i < ChildCount; i++)
            {
                State result = ChildAt(i).Update(blackboard);
                if (result == State.Success) continue;
                SwitchRunningChild(blackboard,
                    result == State.Running ? i : -1);
                return result;
            }
            SwitchRunningChild(blackboard, -1);
            return State.Success;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            base.OnAbort(blackboard);
            if (GetRunningIndex(blackboard) != -1)
                SetRunningIndex(blackboard, -1);
        }

        private void SwitchRunningChild(Blackboard blackboard,
            int nextIndex)
        {
            int runningIndex = GetRunningIndex(blackboard);
            if (runningIndex >= 0 && runningIndex != nextIndex &&
                runningIndex < ChildCount)
                ChildAt(runningIndex).Abort(blackboard);
            if (runningIndex != nextIndex)
                SetRunningIndex(blackboard, nextIndex);
        }

    }
}
