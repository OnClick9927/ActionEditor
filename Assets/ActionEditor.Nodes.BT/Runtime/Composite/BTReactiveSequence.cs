using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("响应序列", "每个 Tick 都从第一个前置子节点重新求值；任一前置项不再成功时，会中止此前运行的后续分支并返回对应状态。"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("Sequence")]
    public sealed class BTReactiveSequence : BTComposite
    {
        [NonSerialized] private int runningIndex;

        protected override void OnInitialized()
        {
            runningIndex = -1;
        }

        protected override void OnStart()
        {
            runningIndex = -1;
        }

        protected override State OnUpdate()
        {
            for (int i = 0; i < ChildCount; i++)
            {
                State result = ChildAt(i).Update();
                if (result == State.Success) continue;
                AbortPreviousRunningChild(i);
                if (result == State.Running) runningIndex = i;
                else runningIndex = -1;
                return result;
            }
            AbortPreviousRunningChild(-1);
            runningIndex = -1;
            return State.Success;
        }

        protected override void OnAbort()
        {
            base.OnAbort();
            runningIndex = -1;
        }

        private void AbortPreviousRunningChild(int nextIndex)
        {
            if (runningIndex >= 0 && runningIndex != nextIndex &&
                runningIndex < ChildCount)
                ChildAt(runningIndex).Abort();
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(runningIndex);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < -1 || value >= ChildCount)
                throw new ArgumentException(
                    "Invalid reactive-sequence runtime status", nameof(values));
            if ((state == State.Running) != (value >= 0))
                throw new ArgumentException(
                    "Reactive-sequence state and running child do not match",
                    nameof(values));
            runningIndex = value;
        }
    }
}
