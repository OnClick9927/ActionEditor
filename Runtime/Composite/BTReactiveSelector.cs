using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("每个 Tick 都从第一个高优先级子节点重新求值；高优先级分支变为可运行或成功时，会中止此前运行的低优先级分支。")]
    [Name("响应选择"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ReactiveSelector")]
    public sealed class BTReactiveSelector : BTComposite
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
                if (result == State.Failure) continue;
                SwitchRunningChild(blackboard,
                    result == State.Running ? i : -1);
                return result;
            }
            SwitchRunningChild(blackboard, -1);
            return State.Failure;
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
