using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("每个 Tick 按固定顺序更新所有未完成分支；任一分支成功时中止其他运行分支并成功，仅当全部分支失败时返回失败。")]
    [Name("并行选择"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ParallelSelector")]

    public class BTParallelSelector : BTComposite
    {
        protected override int RuntimeDataSize => ChildCount;
        protected override int GetMinRuntimeData(int index) =>
            (int)State.Inactive;
        protected override int GetMaxRuntimeData(int index) =>
            (int)State.Running;

        private State GetRunning(Blackboard blackboard, int index) =>
            (State)GetRuntimeData(blackboard, index);
        private void SetRunning(Blackboard blackboard, int index, State value) =>
            SetRuntimeData(blackboard, index, (int)value);

        protected override void OnAbort(Blackboard blackboard)
        {
            base.OnAbort(blackboard);
            for (int i = 0; i < ChildCount; i++)
                SetRunning(blackboard, i, State.Running);
        }
        protected override void OnStart(Blackboard blackboard)
        {
            base.OnStart(blackboard);
            for (int i = 0; i < ChildCount; i++)
                SetRunning(blackboard, i, State.Running);
        }


        protected override State OnUpdate(Blackboard blackboard)
        {
            bool stillRunning = false;
            
            for (int i = 0; i < ChildCount; ++i)
            {
                if (GetRunning(blackboard, i) == State.Running)
                {
                    var status = ChildAt(i).Update(blackboard);
                    if (status == State.Success)
                    {
                        AbortRunningChildren(blackboard);
                        return State.Success;
                    }

                    if (status == State.Running)
                    {
                        stillRunning = true;
                        continue;
                    }
                    SetRunning(blackboard, i, status);
                }
            }

            return stillRunning ? State.Running : State.Failure;
        }

    }
}
