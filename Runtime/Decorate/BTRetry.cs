using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("子节点失败后重新进入，直到成功或耗尽最大尝试次数；失败计数会写入状态快照以支持精确恢复。")]
    [Name("失败重试"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Retry")]
    public sealed class BTRetry : BTDecorateSingle
    {
        [Name("最大尝试次数", "包含首次执行在内的总尝试次数，必须大于零；达到上限后的最后一次失败会直接结束节点。")]
        public int maxAttempts = 1;
        private int GetFailedAttempts(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetFailedAttempts(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => maxAttempts;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (maxAttempts <= 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(maxAttempts)} must be positive");
        }

        protected override void OnStart(Blackboard blackboard)
        {
            SetFailedAttempts(blackboard, 0);
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            State result = child.Update(blackboard);
            if (result != State.Failure) return result;
            int failedAttempts = GetFailedAttempts(blackboard) + 1;
            SetFailedAttempts(blackboard, failedAttempts);
            return failedAttempts < maxAttempts ? State.Running : State.Failure;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            base.OnAbort(blackboard);
            SetFailedAttempts(blackboard, 0);
        }

        protected override State Decorate(Blackboard blackboard, State state) =>
            state;

    }
}
