using ActionAttribute;
using System;
using System.Collections.Generic;

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
        [NonSerialized] private int currentIndex;

        protected override void OnInitialized()
        {
            currentIndex = 0;
        }

        protected override State OnUpdate()
        {
            if (ChildCount == 0) return State.Failure;
            if (currentIndex >= ChildCount) currentIndex = 0;

            State result = ChildAt(currentIndex).Update();
            if (result == State.Running) return State.Running;
            if ((result == State.Success && advanceOnSuccess) ||
                (result == State.Failure && advanceOnFailure))
                currentIndex = (currentIndex + 1) % ChildCount;
            return result;
        }

        protected override void OnAbort()
        {
            base.OnAbort();
            if (resetOnAbort) currentIndex = 0;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(currentIndex);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > Math.Max(0, ChildCount - 1))
                throw new ArgumentException("Invalid round-robin runtime status",
                    nameof(values));
            currentIndex = value;
        }
    }
}
