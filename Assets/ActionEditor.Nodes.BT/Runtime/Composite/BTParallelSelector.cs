using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("并行选择", "每个 Tick 按固定顺序更新所有未完成分支；任一分支成功时中止其他运行分支并成功，仅当全部分支失败时返回失败。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ParallelSelector")]

    public class BTParallelSelector : BTComposite
    {
        [System.NonSerialized] private State[] running;

        protected override void OnAbort()
        {
            base.OnAbort();
            for (int i = 0; i < running.Length; i++)
                running[i] = State.Running;
        }
        protected override void OnStart()
        {
            base.OnStart();
            EnsureRunningState();
            for (int i = 0; i < running.Length; i++)
                running[i] = State.Running;
        }

        private void EnsureRunningState()
        {
            if (running == null || running.Length != ChildCount)
                running = new State[ChildCount];
        }


        protected override State OnUpdate()
        {
            bool stillRunning = false;
            
            for (int i = 0; i < running.Length; ++i)
            {
                if (running[i] == State.Running)
                {
                    var status = ChildAt(i).Update();
                    if (status == State.Success)
                    {
                        AbortRunningChildren();
                        return State.Success;
                    }

                    if (status == State.Running)
                    {
                        stillRunning = true;
                    }
                    running[i] = status;
                }
            }

            return stillRunning ? State.Running : State.Failure;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            int count = ChildCount;
            for (int i = 0; i < count; i++)
                values.Add(running == null
                    ? (int)State.Inactive
                    : (int)running[i]);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            EnsureRunningState();
            for (int i = 0; i < running.Length; i++)
            {
                int value = ReadStatusValue(values, ref index);
                if (value < (int)State.Inactive ||
                    value > (int)State.Running)
                    throw new System.ArgumentException(
                        "Invalid parallel selector runtime status",
                        nameof(values));
                running[i] = (State)value;
            }
        }
    }
}
