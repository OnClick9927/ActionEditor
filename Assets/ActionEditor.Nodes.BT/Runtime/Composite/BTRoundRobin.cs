using ActionAttribute;
using System;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("每次进入只执行当前索引的一个子节点，并根据完成结果决定是否推进；索引会进入状态快照，恢复后继续相同轮询位置。")]
    [Name("轮询"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("RoundRobin")]
    public class BTRoundRobin : BTComposite
    {
        [Name("成功后前进", "当前子节点返回成功时，将下一次进入所用索引推进到后一个子节点，并在末尾循环回第一个。")]
        public bool advanceOnSuccess = true;
        [Name("失败后前进", "当前子节点返回失败时，将下一次进入所用索引推进到后一个子节点；关闭时失败会停留在当前位置。")]
        public bool advanceOnFailure = true;
        [Name("中止时重置", "节点处于运行中并被父级中止时，将已保存的轮询索引恢复为零；关闭时保留中止前的位置。")]
        public bool resetOnAbort;
        private int GetCurrentIndex(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetCurrentIndex(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) =>
            Math.Max(0, ChildCount - 1);

        protected override State OnUpdate(Blackboard blackboard)
        {
            if (ChildCount == 0) return State.Failure;
            int currentIndex = GetCurrentIndex(blackboard);
            if (currentIndex >= ChildCount)
            {
                currentIndex = 0;
                SetCurrentIndex(blackboard, 0);
            }

            State result = ChildAt(currentIndex).Update(blackboard);
            if (result == State.Running) return State.Running;
            if ((result == State.Success && advanceOnSuccess) ||
                (result == State.Failure && advanceOnFailure))
                SetCurrentIndex(blackboard, (currentIndex + 1) % ChildCount);
            return result;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            base.OnAbort(blackboard);
            if (resetOnAbort) SetCurrentIndex(blackboard, 0);
        }

    }
}
